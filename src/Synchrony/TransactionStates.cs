namespace Synchrony;

public enum TransactionStates
{
    New = 1,
    Pending = 2,
    Failed = 3,
    Completed = 4,
    Compensated = 5
}