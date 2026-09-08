using DigitalBank.Application.Interfaces;
using DigitalBank.Domain.Entities;
using DigitalBank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DigitalBank.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly BankDbContext _context;
    public CustomerRepository(BankDbContext context) => _context = context;

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Customers.AsNoTracking().OrderBy(c => c.LastName).ToListAsync(ct);

    public async Task<Customer> AddAsync(Customer customer, CancellationToken ct = default)
    {
        await _context.Customers.AddAsync(customer, ct);
        return customer;
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        await _context.Customers.AnyAsync(c => c.Email == email, ct);
}

public class AccountRepository : IAccountRepository
{
    private readonly BankDbContext _context;
    public AccountRepository(BankDbContext context) => _context = context;

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Account?> GetByIdWithTransactionsAsync(Guid id, CancellationToken ct = default) =>
        await _context.Accounts
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Account>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default) =>
        await _context.Accounts.AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .ToListAsync(ct);

    public async Task<Account> AddAsync(Account account, CancellationToken ct = default)
    {
        await _context.Accounts.AddAsync(account, ct);
        return account;
    }

    public Task UpdateAsync(Account account, CancellationToken ct = default)
    {
        _context.Accounts.Update(account);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByAccountNumberAsync(string accountNumber, CancellationToken ct = default) =>
        await _context.Accounts.AnyAsync(a => a.AccountNumber == accountNumber, ct);
}

public class TransactionRepository : ITransactionRepository
{
    private readonly BankDbContext _context;
    public TransactionRepository(BankDbContext context) => _context = context;

    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken ct = default)
    {
        await _context.Transactions.AddAsync(transaction, ct);
        return transaction;
    }

    public async Task<IReadOnlyList<Transaction>> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default) =>
        await _context.Transactions.AsNoTracking()
            .Where(t => t.AccountId == accountId)
            .ToListAsync(ct);
}

public class AuditLogRepository : IAuditLogRepository
{
    private readonly BankDbContext _context;
    public AuditLogRepository(BankDbContext context) => _context = context;

    public async Task AddAsync(AuditLog log, CancellationToken ct = default) =>
        await _context.AuditLogs.AddAsync(log, ct);
}

public class ApplicationUserRepository : IApplicationUserRepository
{
    private readonly BankDbContext _context;
    public ApplicationUserRepository(BankDbContext context) => _context = context;

    public async Task<ApplicationUser?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task<ApplicationUser> AddAsync(ApplicationUser user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
        return user;
    }

    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default) =>
        await _context.Users.AnyAsync(u => u.Username == username, ct);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly BankDbContext _context;
    public UnitOfWork(BankDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
