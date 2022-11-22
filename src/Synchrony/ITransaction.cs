namespace Synchrony;

using Configuration;

public interface ITransaction
{
    Task Execute(CancellationToken cancellationToken = default);

    ITransaction AddOperations(IOperationBuilder builder, params IOperationBuilder[] builders);

    ITransaction Configure(Action<TransactionConfigurator> configurator);

    ITransaction Configure();

    Guid GetTransactionId();
}