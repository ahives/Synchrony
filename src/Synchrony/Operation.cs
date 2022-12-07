namespace Synchrony;

using MassTransit;
using Microsoft.Extensions.Logging;
using Configuration;

public abstract class Operation<TOperation> :
    IOperation
{
    private readonly ILogger<Operation<TOperation>> _logger;

    protected Operation(ILogger<Operation<TOperation>> logger)
    {
        _logger = logger;
    }

    protected Operation()
    {
    }

    public virtual OperationConfig Configure() => OperationConfigCache.Default;

    public virtual string GetName() => typeof(TOperation).FullName ?? throw new InvalidOperationException();

    public Guid GetId() => NewId.NextGuid();

    public abstract Task<bool> Execute();

    public virtual async Task<bool> Compensate()
    {
        _logger.LogInformation("You forgot to add compensation logic");
        
        return await Task.FromResult(true);
    }

}

public static class Operation
{
    public static IOperation Create<T>()
        where T : class, IOperation
    {
        if (typeof(T).IsAssignableTo(typeof(IOperation)))
            return CreateInstance(typeof(T));

        return OperationsCache.Empty;
    }

    static IOperation CreateInstance(Type type)
    {
        try
        {
            return Activator.CreateInstance(type) as IOperation ?? OperationsCache.Empty;
        }
        catch (Exception e)
        {
            return OperationsCache.Empty;
        }
    }
}