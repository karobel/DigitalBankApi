using DigitalBank.Application.DTOs;
using DigitalBank.Application.Interfaces;
using DigitalBank.Domain.Entities;
using DigitalBank.Domain.Enums;
using DigitalBank.Domain.Exceptions;

namespace DigitalBank.Application.Services;

/// <summary>
/// Core banking operations: account creation, deposits, withdrawals, and transfers.
/// All money-moving operations are wrapped in a single SaveChanges call (via IUnitOfWork)
/// so a transfer either fully succeeds or fully fails — never leaves accounts half-updated.
/// </summary>
public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository,
        ITransactionRepository transactionRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _transactionRepository = transactionRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AccountDto> CreateAsync(CreateAccountDto dto, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(dto.CustomerId, ct)
            ?? throw new CustomerNotFoundException(dto.CustomerId);

        var accountNumber = await GenerateUniqueAccountNumberAsync(ct);

        var account = new Account
        {
            CustomerId = customer.Id,
            AccountNumber = accountNumber,
            Type = dto.Type,
            Currency = dto.Currency,
            Balance = 0m,
            Status = AccountStatus.Active
        };

        await _accountRepository.AddAsync(account, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto(account);
    }

    public async Task<AccountDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByIdAsync(id, ct);
        return account is null ? null : ToDto(account);
    }

    public async Task<IReadOnlyList<AccountDto>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        var accounts = await _accountRepository.GetByCustomerIdAsync(customerId, ct);
        return accounts.Select(ToDto).ToList();
    }

    public async Task<TransactionDto> DepositAsync(Guid accountId, DepositDto dto, CancellationToken ct = default)
    {
        if (dto.Amount <= 0)
        {
            throw new InvalidTransferException("Deposit amount must be greater than zero.");
        }

        var account = await _accountRepository.GetByIdAsync(accountId, ct)
            ?? throw new AccountNotFoundException(accountId);

        EnsureAccountIsActive(account);

        account.Balance += dto.Amount;

        var transaction = new Transaction
        {
            AccountId = account.Id,
            Type = TransactionType.Deposit,
            Amount = dto.Amount,
            BalanceAfter = account.Balance,
            Description = dto.Description ?? "Deposit"
        };

        await _transactionRepository.AddAsync(transaction, ct);
        await _accountRepository.UpdateAsync(account, ct);
        await LogAuditAsync("Deposit", "Account", account.Id.ToString(),
            $"Deposited {dto.Amount:C} into {account.AccountNumber}", ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto(transaction);
    }

    public async Task<TransactionDto> WithdrawAsync(Guid accountId, WithdrawDto dto, CancellationToken ct = default)
    {
        if (dto.Amount <= 0)
        {
            throw new InvalidTransferException("Withdrawal amount must be greater than zero.");
        }

        var account = await _accountRepository.GetByIdAsync(accountId, ct)
            ?? throw new AccountNotFoundException(accountId);

        EnsureAccountIsActive(account);

        if (account.Balance < dto.Amount)
        {
            throw new InsufficientFundsException(account.AccountNumber, dto.Amount, account.Balance);
        }

        account.Balance -= dto.Amount;

        var transaction = new Transaction
        {
            AccountId = account.Id,
            Type = TransactionType.Withdrawal,
            Amount = dto.Amount,
            BalanceAfter = account.Balance,
            Description = dto.Description ?? "Withdrawal"
        };

        await _transactionRepository.AddAsync(transaction, ct);
        await _accountRepository.UpdateAsync(account, ct);
        await LogAuditAsync("Withdrawal", "Account", account.Id.ToString(),
            $"Withdrew {dto.Amount:C} from {account.AccountNumber}", ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto(transaction);
    }

    public async Task<IReadOnlyList<TransactionDto>> TransferAsync(TransferDto dto, CancellationToken ct = default)
    {
        if (dto.Amount <= 0)
        {
            throw new InvalidTransferException("Transfer amount must be greater than zero.");
        }

        if (dto.FromAccountId == dto.ToAccountId)
        {
            throw new InvalidTransferException("Cannot transfer to the same account.");
        }

        var fromAccount = await _accountRepository.GetByIdAsync(dto.FromAccountId, ct)
            ?? throw new AccountNotFoundException(dto.FromAccountId);
        var toAccount = await _accountRepository.GetByIdAsync(dto.ToAccountId, ct)
            ?? throw new AccountNotFoundException(dto.ToAccountId);

        EnsureAccountIsActive(fromAccount);
        EnsureAccountIsActive(toAccount);

        if (fromAccount.Balance < dto.Amount)
        {
            throw new InsufficientFundsException(fromAccount.AccountNumber, dto.Amount, fromAccount.Balance);
        }

        fromAccount.Balance -= dto.Amount;
        toAccount.Balance += dto.Amount;

        var transferGroupId = Guid.NewGuid();

        var debit = new Transaction
        {
            AccountId = fromAccount.Id,
            Type = TransactionType.TransferOut,
            Amount = dto.Amount,
            BalanceAfter = fromAccount.Balance,
            Description = dto.Description ?? $"Transfer to {toAccount.AccountNumber}",
            RelatedTransferId = transferGroupId
        };

        var credit = new Transaction
        {
            AccountId = toAccount.Id,
            Type = TransactionType.TransferIn,
            Amount = dto.Amount,
            BalanceAfter = toAccount.Balance,
            Description = dto.Description ?? $"Transfer from {fromAccount.AccountNumber}",
            RelatedTransferId = transferGroupId
        };

        await _transactionRepository.AddAsync(debit, ct);
        await _transactionRepository.AddAsync(credit, ct);
        await _accountRepository.UpdateAsync(fromAccount, ct);
        await _accountRepository.UpdateAsync(toAccount, ct);

        await LogAuditAsync("Transfer", "Account", fromAccount.Id.ToString(),
            $"Transferred {dto.Amount:C} from {fromAccount.AccountNumber} to {toAccount.AccountNumber}", ct);

        // Single SaveChanges call → both legs of the transfer commit atomically.
        await _unitOfWork.SaveChangesAsync(ct);

        return new List<TransactionDto> { ToDto(debit), ToDto(credit) };
    }

    public async Task<IReadOnlyList<TransactionDto>> GetTransactionHistoryAsync(Guid accountId, CancellationToken ct = default)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, ct)
            ?? throw new AccountNotFoundException(accountId);

        var transactions = await _transactionRepository.GetByAccountIdAsync(account.Id, ct);
        return transactions.OrderByDescending(t => t.CreatedAtUtc).Select(ToDto).ToList();
    }

    private static void EnsureAccountIsActive(Account account)
    {
        if (account.Status != AccountStatus.Active)
        {
            throw new AccountNotActiveException(account.AccountNumber);
        }
    }

    private async Task<string> GenerateUniqueAccountNumberAsync(CancellationToken ct)
    {
        string candidate;
        do
        {
            candidate = $"CA{Random.Shared.Next(100_000_000, 999_999_999)}";
        }
        while (await _accountRepository.ExistsByAccountNumberAsync(candidate, ct));

        return candidate;
    }

    private async Task LogAuditAsync(string action, string entityName, string entityId, string details, CancellationToken ct)
    {
        await _auditLogRepository.AddAsync(new AuditLog
        {
            UserId = "system", // Replaced with the authenticated user id once HttpContext access is wired in.
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details
        }, ct);
    }

    private static AccountDto ToDto(Account a) =>
        new(a.Id, a.AccountNumber, a.CustomerId, a.Type, a.Balance, a.Currency, a.Status, a.CreatedAtUtc);

    private static TransactionDto ToDto(Transaction t) =>
        new(t.Id, t.AccountId, t.Type, t.Amount, t.BalanceAfter, t.Description, t.CreatedAtUtc);
}
