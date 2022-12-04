namespace Synchrony.Extensions;

internal static class ForEachExtensions
{
    internal static void ForEach(this IReadOnlyList<IObserver<TransactionContext>> observers, Action<IObserver<TransactionContext>> action)
    {
        for (int i = 0; i < observers.Count; i++)
            action(observers[i]);
    }
}