namespace Synchrony;

public enum TransactionState
{
    New = 1,
    Pending = 2,
    Failed = 3,
    Completed = 4,
    Compensated = 5
}