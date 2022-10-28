namespace Synchrony;

public static class Operation
{
    public static IOperationBuilder Create<T>()
        where T : class, IOperationBuilder
    {
        if (typeof(T).IsAssignableTo(typeof(IOperationBuilder)))
            return CreateInstance(typeof(T));

        return OperationsCache.Empty;
    }

    static IOperationBuilder CreateInstance(Type type)
    {
        try
        {
            return Activator.CreateInstance(type) as IOperationBuilder ?? OperationsCache.Empty;
        }
        catch (Exception e)
        {
            return OperationsCache.Empty;
        }
    }
}