namespace Synchrony;

using Configuration;

public interface ITransaction
{
    void Execute();

    Transaction AddOperations(IOperationBuilder builder, params IOperationBuilder[] builders);

    Transaction Configure(Action<TransactionConfigurator> configurator);

    Transaction Configure();
}