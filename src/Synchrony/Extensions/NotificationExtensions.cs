namespace Synchrony.Extensions;

public static class NotificationExtensions
{
    public static void SendToObservers<TContext>(this List<IObserver<TContext>> observers, TContext context) =>
        observers.ForEach(0, x =>
        {
            switch (context)
            {
                case TransactionContext ctx:
                    HandleTransactionObserver(ctx, (IObserver<TransactionContext>)x);
                    break;
                
                case OperationContext ctx:
                    HandleOperationObserver(ctx, (IObserver<OperationContext>)x);
                    break;
            }
        });

    static void HandleOperationObserver(OperationContext context, IObserver<OperationContext> observer)
    {
        switch (context.State)
        {
            case OperationState.New:
            case OperationState.Pending:
            case OperationState.Completed:
            case OperationState.Compensated:
                observer.OnNext(context);
                break;
            case OperationState.Failed:
            default:
                observer.OnError(new TransactionPersistenceException());
                break;
        }
    }

    static void HandleTransactionObserver(TransactionContext context, IObserver<TransactionContext> observer)
    {
        switch (context.State)
        {
            case TransactionState.New:
            case TransactionState.Pending:
            case TransactionState.Completed:
            case TransactionState.Compensated:
                observer.OnNext(context);
                break;
            case TransactionState.Failed:
            default:
                observer.OnError(new TransactionPersistenceException());
                break;
        }
    }
}