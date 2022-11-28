namespace Synchrony.Configuration;

public interface TransactionConfigurator
{
    void TurnOnLogging();

    void Retry(TransactionRetry retry = TransactionRetry.None);

    void Subscribe(IObserver<TransactionContext> observer, params IObserver<TransactionContext>[] observers);
}