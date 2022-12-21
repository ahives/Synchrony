namespace Synchrony;

using MassTransit;
using Microsoft.Extensions.Logging;
using Configuration;

public abstract class Operation<TOperation> :
    IOperation
{
    private readonly ILogger<Operation<TOperation>> _logger;
    private bool _disposed;

    public OperationMetadata Metadata { get; init; }

    protected Operation(ILogger<Operation<TOperation>> logger) : this()
    {
        _logger = logger;
    }

    protected Operation()
    {
        Metadata = new()
            {Id = NewId.NextGuid(), Name = typeof(TOperation).FullName ?? throw new InvalidOperationException()};
    }

    public virtual OperationConfig Configure() => OperationConfigCache.Default;

    public abstract Task<bool> Execute();

    public virtual async Task<bool> Compensate()
    {
        _logger.LogInformation("You forgot to add compensation logic");
        
        return await Task.FromResult(true);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            DisposeManagedResources();

        DisposeUnmanagedResources();

        _disposed = true;
    }

    protected virtual void DisposeUnmanagedResources(){}

    protected virtual void DisposeManagedResources(){}
}

public static class Operation
{
    public static IOperation Create<T>(params object[] args)
        where T : class, IOperation
    {
        if (typeof(T).IsAssignableTo(typeof(IOperation)))
            return CreateInstance(typeof(T), args);

        return OperationsCache.Empty;
    }

    static IOperation CreateInstance(Type type, object[] args)
    {
        try
        {
            return Activator.CreateInstance(type, args) as IOperation ?? OperationsCache.Empty;
        }
        catch
        {
            return OperationsCache.Empty;
        }
    }
}