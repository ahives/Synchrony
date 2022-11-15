namespace Synchrony;

public static class Observer
{
    public static T Create<T>()
    {
        return CreateInstance<T>(typeof(T));
    }

    static T? CreateInstance<T>(Type type)
    {
        try
        {
            return (T) Activator.CreateInstance(type)!;
        }
        catch
        {
            return default;
        }
    }
}