using DigitalBank.Application.DTOs;

namespace DigitalBank.Application.Interfaces;

public interface ICustomerService
{
    Task<CustomerDto> CreateAsync(CreateCustomerDto dto, CancellationToken ct = default);
    Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default);
}

public interface IAccountService
{
    Task<AccountDto> CreateAsync(CreateAccountDto dto, CancellationToken ct = default);
    Task<AccountDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AccountDto>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<TransactionDto> DepositAsync(Guid accountId, DepositDto dto, CancellationToken ct = default);
    Task<TransactionDto> WithdrawAsync(Guid accountId, WithdrawDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<TransactionDto>> TransferAsync(TransferDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<TransactionDto>> GetTransactionHistoryAsync(Guid accountId, CancellationToken ct = default);
}

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto, CancellationToken ct = default);
}
