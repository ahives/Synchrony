namespace Synchrony.Extensions;

public static class NotificationExtensions
{
    public static void SendToSubscribers(this IEnumerable<IObserver<TransactionContext>> subscribers, TransactionContext context) =>
        subscribers.ForEach(subscriber =>
        {
            switch (context.State)
            {
                case TransactionStates.New:
                case TransactionStates.Pending:
                case TransactionStates.Completed:
                case TransactionStates.Compensated:
                    subscriber.OnNext(context);
                    break;
                case TransactionStates.Failed:
                default:
                    subscriber.OnError(new TransactionPersistenceException());
                    break;
            }
        });
}