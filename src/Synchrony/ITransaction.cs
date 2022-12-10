namespace Synchrony;

using Configuration;

public interface ITransaction
{
    Task Execute(CancellationToken cancellationToken = default);

    ITransaction AddOperations(IOperation operation, params IOperation[] operations);

    ITransaction Configure(Action<TransactionConfigurator> configurator);

    ITransaction Configure();

    Guid GetTransactionId();

    IEnumerable<IObserver<TransactionContext>> GetSubscribers();
}