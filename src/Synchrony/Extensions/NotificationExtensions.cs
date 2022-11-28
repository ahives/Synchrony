namespace Synchrony.Extensions;

public static class NotificationExtensions
{
    public static void SendToObservers(this IReadOnlyList<IObserver<TransactionContext>> observers, TransactionContext context) =>
        observers.ForEach(observer =>
        {
            switch (context.State)
            {
                case TransactionStates.New:
                case TransactionStates.Pending:
                case TransactionStates.Completed:
                case TransactionStates.Compensated:
                    observer.OnNext(context);
                    break;
                case TransactionStates.Failed:
                default:
                    observer.OnError(new TransactionPersistenceException());
                    break;
            }
        });
}