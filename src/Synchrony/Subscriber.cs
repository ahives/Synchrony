namespace Synchrony;

public static class Subscriber
{
    public static T Create<T>(params object[] args)
    {
        return CreateInstance<T>(typeof(T), args);
    }

    static T? CreateInstance<T>(Type type, object[] args)
    {
        try
        {
            return (T) Activator.CreateInstance(type, args)!;
        }
        catch
        {
            return default;
        }
    }
}