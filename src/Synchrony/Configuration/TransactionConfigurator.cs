namespace Synchrony.Configuration;

public interface TransactionConfigurator
{
    void Retry(TransactionRetry retry = TransactionRetry.None);

    void Subscribe(IObserver<TransactionContext> subscriber, params IObserver<TransactionContext>[] subscribers);
}