namespace Synchrony.Extensions;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static class ForEachExtensions
{
    internal static void ForEach(this IEnumerable<IObserver<TransactionContext>> subscribers, Action<IObserver<TransactionContext>> action)
    {
        Span<IObserver<TransactionContext>> memory = CollectionsMarshal.AsSpan(subscribers.ToList());
        ref var ptr = ref MemoryMarshal.GetReference(memory);

        for (int i = 0; i < memory.Length; i++)
        {
            IObserver<TransactionContext> item = Unsafe.Add(ref ptr, i);
            action(item);
        }
    }
}