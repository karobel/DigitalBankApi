using DigitalBank.Domain.Enums;

namespace DigitalBank.Application.DTOs;

public record CustomerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    DateTime DateOfBirth);

public record CreateCustomerDto(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    DateTime DateOfBirth);

public record AccountDto(
    Guid Id,
    string AccountNumber,
    Guid CustomerId,
    AccountType Type,
    decimal Balance,
    string Currency,
    AccountStatus Status,
    DateTime CreatedAtUtc);

public record CreateAccountDto(
    Guid CustomerId,
    AccountType Type,
    string Currency = "CAD");

public record TransactionDto(
    Guid Id,
    Guid AccountId,
    TransactionType Type,
    decimal Amount,
    decimal BalanceAfter,
    string? Description,
    DateTime CreatedAtUtc);

public record DepositDto(decimal Amount, string? Description);

public record WithdrawDto(decimal Amount, string? Description);

public record TransferDto(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    string? Description);

public record LoginRequestDto(string Username, string Password);

public record RegisterUserDto(string Username, string Email, string Password);

public record AuthResponseDto(string Token, DateTime ExpiresAtUtc, string Username, string Role);
