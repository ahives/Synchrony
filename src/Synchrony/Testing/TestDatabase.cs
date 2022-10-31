namespace Synchrony.Testing;

using Persistence;

public static class TestDatabase
{
    private static IPersistenceProvider _provider;
    private static readonly object obj = new();

    public static IPersistenceProvider Provider
    {
        get
        {
            lock (obj)
            {
                if (_provider == null)
                    _provider = new TestPersistenceProvider();

                return _provider;
            }
        }
    }
}