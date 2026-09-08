namespace DigitalBank.Domain.Enums;

public enum AccountType
{
    Checking = 0,
    Savings = 1,
    Business = 2
}

public enum AccountStatus
{
    Active = 0,
    Frozen = 1,
    Closed = 2
}

public enum TransactionType
{
    Deposit = 0,
    Withdrawal = 1,
    TransferOut = 2,
    TransferIn = 3
}
