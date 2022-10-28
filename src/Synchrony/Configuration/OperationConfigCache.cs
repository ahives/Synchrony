namespace Synchrony.Configuration;

public static class OperationConfigCache
{
    public static readonly OperationConfig Default = new()
    {
        TransactionRetry = TransactionRetry.None
    };
}