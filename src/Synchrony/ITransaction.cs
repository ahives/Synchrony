namespace Synchrony;

using Configuration;

public interface ITransaction
{
    Task Execute(CancellationToken cancellationToken = default);
    
    Task Execute(Guid transactionId, CancellationToken cancellationToken = default);

    ITransaction AddOperations(IOperation operation, params IOperation[] operations);

    ITransaction Configure(Action<TransactionConfigurator> configurator);

    ITransaction Configure();

    Guid GetTransactionId();

    IEnumerable<IObserver<TransactionContext>> GetSubscribers();

    TransactionMetadata Metadata { get; }
}

public record TransactionMetadata
{
    public Guid Id { get; init; }
    
    public string Hash { get; init; }

    public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
}