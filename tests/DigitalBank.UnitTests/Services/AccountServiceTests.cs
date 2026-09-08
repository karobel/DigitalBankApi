using DigitalBank.Application.DTOs;
using DigitalBank.Application.Interfaces;
using DigitalBank.Application.Services;
using DigitalBank.Domain.Entities;
using DigitalBank.Domain.Enums;
using DigitalBank.Domain.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace DigitalBank.UnitTests.Services;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepoMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<ITransactionRepository> _transactionRepoMock = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        _sut = new AccountService(
            _accountRepoMock.Object,
            _customerRepoMock.Object,
            _transactionRepoMock.Object,
            _auditLogRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    private static Account ActiveAccount(decimal balance = 100m) => new()
    {
        Id = Guid.NewGuid(),
        AccountNumber = "CA123456789",
        Balance = balance,
        Status = AccountStatus.Active,
        Currency = "CAD"
    };

    // ───────────────────────── Deposit ─────────────────────────

    [Fact]
    public async Task DepositAsync_WithValidAmount_IncreasesBalanceAndRecordsTransaction()
    {
        var account = ActiveAccount(balance: 100m);
        _accountRepoMock.Setup(r => r.GetByIdAsync(account.Id, default)).ReturnsAsync(account);

        var result = await _sut.DepositAsync(account.Id, new DepositDto(50m, "Test deposit"));

        result.Amount.Should().Be(50m);
        result.BalanceAfter.Should().Be(150m);
        account.Balance.Should().Be(150m);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DepositAsync_WithZeroOrNegativeAmount_ThrowsInvalidTransferException()
    {
        var account = ActiveAccount();
        _accountRepoMock.Setup(r => r.GetByIdAsync(account.Id, default)).ReturnsAsync(account);

        var act = async () => await _sut.DepositAsync(account.Id, new DepositDto(0m, null));

        await act.Should().ThrowAsync<InvalidTransferException>();
    }

    [Fact]
    public async Task DepositAsync_WhenAccountNotFound_ThrowsAccountNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _accountRepoMock.Setup(r => r.GetByIdAsync(missingId, default)).ReturnsAsync((Account?)null);

        var act = async () => await _sut.DepositAsync(missingId, new DepositDto(50m, null));

        await act.Should().ThrowAsync<AccountNotFoundException>();
    }

    [Fact]
    public async Task DepositAsync_WhenAccountFrozen_ThrowsAccountNotActiveException()
    {
        var account = ActiveAccount();
        account.Status = AccountStatus.Frozen;
        _accountRepoMock.Setup(r => r.GetByIdAsync(account.Id, default)).ReturnsAsync(account);

        var act = async () => await _sut.DepositAsync(account.Id, new DepositDto(50m, null));

        await act.Should().ThrowAsync<AccountNotActiveException>();
    }

    // ───────────────────────── Withdraw ─────────────────────────

    [Fact]
    public async Task WithdrawAsync_WithSufficientFunds_DecreasesBalance()
    {
        var account = ActiveAccount(balance: 200m);
        _accountRepoMock.Setup(r => r.GetByIdAsync(account.Id, default)).ReturnsAsync(account);

        var result = await _sut.WithdrawAsync(account.Id, new WithdrawDto(75m, null));

        result.BalanceAfter.Should().Be(125m);
        account.Balance.Should().Be(125m);
    }

    [Fact]
    public async Task WithdrawAsync_WithInsufficientFunds_ThrowsInsufficientFundsException()
    {
        var account = ActiveAccount(balance: 30m);
        _accountRepoMock.Setup(r => r.GetByIdAsync(account.Id, default)).ReturnsAsync(account);

        var act = async () => await _sut.WithdrawAsync(account.Id, new WithdrawDto(100m, null));

        await act.Should().ThrowAsync<InsufficientFundsException>();
        account.Balance.Should().Be(30m); // unchanged
    }

    // ───────────────────────── Transfer ─────────────────────────

    [Fact]
    public async Task TransferAsync_WithSufficientFunds_MovesMoneyBetweenAccounts()
    {
        var fromAccount = ActiveAccount(balance: 500m);
        var toAccount = ActiveAccount(balance: 100m);

        _accountRepoMock.Setup(r => r.GetByIdAsync(fromAccount.Id, default)).ReturnsAsync(fromAccount);
        _accountRepoMock.Setup(r => r.GetByIdAsync(toAccount.Id, default)).ReturnsAsync(toAccount);

        var result = await _sut.TransferAsync(new TransferDto(fromAccount.Id, toAccount.Id, 150m, "Rent"));

        fromAccount.Balance.Should().Be(350m);
        toAccount.Balance.Should().Be(250m);
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Type == TransactionType.TransferOut && t.Amount == 150m);
        result.Should().Contain(t => t.Type == TransactionType.TransferIn && t.Amount == 150m);
    }

    [Fact]
    public async Task TransferAsync_WithInsufficientFunds_ThrowsAndLeavesBalancesUnchanged()
    {
        var fromAccount = ActiveAccount(balance: 50m);
        var toAccount = ActiveAccount(balance: 100m);

        _accountRepoMock.Setup(r => r.GetByIdAsync(fromAccount.Id, default)).ReturnsAsync(fromAccount);
        _accountRepoMock.Setup(r => r.GetByIdAsync(toAccount.Id, default)).ReturnsAsync(toAccount);

        var act = async () => await _sut.TransferAsync(new TransferDto(fromAccount.Id, toAccount.Id, 500m, null));

        await act.Should().ThrowAsync<InsufficientFundsException>();
        fromAccount.Balance.Should().Be(50m);
        toAccount.Balance.Should().Be(100m);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task TransferAsync_ToSameAccount_ThrowsInvalidTransferException()
    {
        var account = ActiveAccount(balance: 100m);

        var act = async () => await _sut.TransferAsync(new TransferDto(account.Id, account.Id, 10m, null));

        await act.Should().ThrowAsync<InvalidTransferException>();
    }

    [Fact]
    public async Task TransferAsync_WhenDestinationAccountFrozen_ThrowsAccountNotActiveException()
    {
        var fromAccount = ActiveAccount(balance: 500m);
        var toAccount = ActiveAccount(balance: 100m);
        toAccount.Status = AccountStatus.Frozen;

        _accountRepoMock.Setup(r => r.GetByIdAsync(fromAccount.Id, default)).ReturnsAsync(fromAccount);
        _accountRepoMock.Setup(r => r.GetByIdAsync(toAccount.Id, default)).ReturnsAsync(toAccount);

        var act = async () => await _sut.TransferAsync(new TransferDto(fromAccount.Id, toAccount.Id, 50m, null));

        await act.Should().ThrowAsync<AccountNotActiveException>();
    }
}
