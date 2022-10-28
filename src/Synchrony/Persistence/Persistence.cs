namespace Synchrony.Persistence;

public static class Persistence
{
    private static IPersistenceProvider _provider;
    private static readonly object _obj = new();

    public static IPersistenceProvider Provider
    {
        get
        {
            lock (_obj)
            {
                if (_provider == null)
                    _provider = new PersistenceProvider();

                return _provider;
            }
        }
    }
}