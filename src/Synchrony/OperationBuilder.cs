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

    public TransactionOperation Create(Guid transactionId, int sequenceNumber) =>
        new()
        {
            TransactionId = transactionId,
            OperationId = NewId.NextGuid(),
            Name = GetName(),
            SequenceNumber = sequenceNumber,
            Work = DoWork(),
            Compensation = Compensate(),
            Config = Configure()
        };

    protected virtual OperationConfig Configure() => OperationConfigCache.Default;

    protected virtual string GetName() => typeof(TOperation).FullName ?? throw new InvalidOperationException();

    protected virtual Action Compensate() => () =>
    {
        // _logger.LogDebug("You forgot to add compensation logic");
    };

    protected abstract Func<bool> DoWork();
}