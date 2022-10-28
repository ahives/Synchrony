namespace Synchrony.Configuration;

public static class SynchronyConfigCache
{
    public static readonly TransactionConfig Default = new()
    {
        ConsoleLoggingOn = true
    };
}