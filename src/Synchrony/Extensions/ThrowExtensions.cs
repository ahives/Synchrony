namespace Synchrony.Extensions;

using CommunityToolkit.Diagnostics;

public static class ThrowExtensions
{
    internal static void ThrowIfFailed(this Func<TransactionOperation, bool> function, TransactionOperation operation,
        OperationState state, List<IObserver<OperationContext>> observers) =>
        ThrowIfFailed(function,
            operation,
            _ => new OperationContext
                {OperationId = operation.OperationId, TransactionId = operation.TransactionId, State = state},
            observers);

    internal static void ThrowIfFailed(this Func<Guid, bool> function, Guid transactionId, TransactionState state,
        List<IObserver<TransactionContext>> observers) =>
        ThrowIfFailed(function,
            transactionId,
            _ => new TransactionContext {TransactionId = transactionId, State = state},
            observers);

    internal static void ThrowIfFailed(this Func<Guid, TransactionState, bool> function, Guid transactionId,
        TransactionState state, List<IObserver<TransactionContext>> observers) =>
        ThrowIfFailed(function,
            transactionId,
            state,
            _ => new TransactionContext {TransactionId = transactionId, State = state},
            observers);

    internal static void ThrowIfFailed(this Func<Guid, OperationState, bool> function, Guid operationId,
        Guid transactionId, OperationState state, List<IObserver<OperationContext>> observers) =>
        ThrowIfFailed(function,
            transactionId,
            state,
            _ => new OperationContext {OperationId = operationId, TransactionId = transactionId, State = state},
            observers);

    static void ThrowIfFailed<T, TContext>(Func<T, bool> function, T input, Func<T, TContext> contextBuilder,
        List<IObserver<TContext>> observers)
    {
        Guard.IsNotNull(function);
        Guard.IsNotNull(input);
        Guard.IsNotNull(observers);

        bool success = function.Invoke(input);
        var context = contextBuilder.Invoke(input);

        if (!success)
        {
            observers.SendToObservers(context);
            throw new TransactionPersistenceException();
        }

        observers.SendToObservers(context);
    }

    static void ThrowIfFailed<T, TState, TContext>(Func<T, TState, bool> function, T input, TState state, Func<T, TContext> contextBuilder,
        List<IObserver<TContext>> observers)
    {
        Guard.IsNotNull(function);
        Guard.IsNotNull(input);
        Guard.IsNotNull(observers);

        bool success = function.Invoke(input, state);
        var context = contextBuilder.Invoke(input);

        if (!success)
        {
            observers.SendToObservers(context);
            throw new TransactionPersistenceException();
        }

        observers.SendToObservers(context);
    }
}