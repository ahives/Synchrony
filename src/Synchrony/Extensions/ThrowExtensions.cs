namespace Synchrony.Extensions;

using CommunityToolkit.Diagnostics;

internal static class ThrowExtensions
{
    internal static void ThrowIfFailed(this Func<Guid, bool> function, Guid transactionId, TransactionStates state,
        List<IObserver<TransactionContext>> observers) =>
        ThrowIfFailed(function,
            transactionId,
            _ => new TransactionContext {TransactionId = transactionId, State = state},
            observers);

    internal static void ThrowIfFailed(this Func<Guid, TransactionStates, bool> function, Guid transactionId,
        TransactionStates state, List<IObserver<TransactionContext>> observers) =>
        ThrowIfFailed(function,
            transactionId,
            state,
            _ => new TransactionContext {TransactionId = transactionId, State = state},
            observers);

    static void ThrowIfFailed<T>(Func<T, bool> function, T input, Func<T, TransactionContext> contextBuilder,
        List<IObserver<TransactionContext>> observers)
    {
        Guard.IsNotNull(function);
        Guard.IsNotNull(input);
        Guard.IsNotNull(observers);

        bool success = function.Invoke(input);
        var context = contextBuilder.Invoke(input);

        observers.SendToSubscribers(context);

        if (!success)
            throw new TransactionPersistenceException();
    }

    static void ThrowIfFailed<T, TState>(Func<T, TState, bool> function, T input, TState state, Func<T, TransactionContext> contextBuilder,
        List<IObserver<TransactionContext>> observers)
    {
        Guard.IsNotNull(function);
        Guard.IsNotNull(input);
        Guard.IsNotNull(observers);

        bool success = function.Invoke(input, state);
        var context = contextBuilder.Invoke(input);

        observers.SendToSubscribers(context);

        if (!success)
            throw new TransactionPersistenceException();
    }
}