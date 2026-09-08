using DigitalBank.Application.DTOs;
using DigitalBank.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalBank.Api.Controllers;

[ApiController]
[Route("api/v1/accounts")]
[Produces("application/json")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService) => _accountService = accountService;

    /// <summary>Returns a single account by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDto>> GetById(Guid id, CancellationToken ct)
    {
        var account = await _accountService.GetByIdAsync(id, ct);
        return account is null ? NotFound() : Ok(account);
    }

    /// <summary>Returns all accounts belonging to a given customer.</summary>
    [HttpGet("customer/{customerId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<AccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetByCustomer(Guid customerId, CancellationToken ct)
    {
        var accounts = await _accountService.GetByCustomerIdAsync(customerId, ct);
        return Ok(accounts);
    }

    /// <summary>Opens a new account for a customer.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDto>> Create(CreateAccountDto dto, CancellationToken ct)
    {
        var created = await _accountService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Deposits funds into an account.</summary>
    [HttpPost("{id:guid}/deposit")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> Deposit(Guid id, DepositDto dto, CancellationToken ct)
    {
        var transaction = await _accountService.DepositAsync(id, dto, ct);
        return Ok(transaction);
    }

    /// <summary>Withdraws funds from an account.</summary>
    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> Withdraw(Guid id, WithdrawDto dto, CancellationToken ct)
    {
        var transaction = await _accountService.WithdrawAsync(id, dto, ct);
        return Ok(transaction);
    }

    /// <summary>Transfers funds between two accounts. Both legs commit atomically.</summary>
    [HttpPost("transfer")]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> Transfer(TransferDto dto, CancellationToken ct)
    {
        var transactions = await _accountService.TransferAsync(dto, ct);
        return Ok(transactions);
    }

    /// <summary>Returns the transaction history for an account, most recent first.</summary>
    [HttpGet("{id:guid}/transactions")]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> GetTransactions(Guid id, CancellationToken ct)
    {
        var transactions = await _accountService.GetTransactionHistoryAsync(id, ct);
        return Ok(transactions);
    }
}
