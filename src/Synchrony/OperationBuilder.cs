namespace Synchrony;

using MassTransit;
using Microsoft.Extensions.Logging;
using Configuration;

public abstract class OperationBuilder<TOperation> :
    IOperationBuilder
{
    private readonly ILogger<OperationBuilder<TOperation>> _logger;

    protected OperationBuilder(ILogger<OperationBuilder<TOperation>> logger)
    {
        _logger = logger;
    }

    protected OperationBuilder()
    {
    }

    public virtual OperationConfig Configure() => OperationConfigCache.Default;

    public virtual string GetName() => typeof(TOperation).FullName ?? throw new InvalidOperationException();

    public Guid GetId() => NewId.NextGuid();

    public abstract Func<bool> DoWork();

    public virtual Action OnFailure() => () =>
    {
        _logger.LogInformation("You forgot to add compensation logic");
    };
}