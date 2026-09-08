namespace DigitalBank.Domain.Exceptions;

/// <summary>Base type for all domain/business rule violations.</summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

public class InsufficientFundsException : DomainException
{
    public InsufficientFundsException(string accountNumber, decimal requested, decimal available)
        : base($"Account {accountNumber} has insufficient funds. Requested: {requested:C}, Available: {available:C}.")
    {
    }
}

public class AccountNotFoundException : DomainException
{
    public AccountNotFoundException(Guid accountId)
        : base($"Account with ID '{accountId}' was not found.")
    {
    }
}

public class AccountNotActiveException : DomainException
{
    public AccountNotActiveException(string accountNumber)
        : base($"Account {accountNumber} is not active and cannot process transactions.")
    {
    }
}

public class CustomerNotFoundException : DomainException
{
    public CustomerNotFoundException(Guid customerId)
        : base($"Customer with ID '{customerId}' was not found.")
    {
    }
}

public class InvalidTransferException : DomainException
{
    public InvalidTransferException(string message) : base(message) { }
}
