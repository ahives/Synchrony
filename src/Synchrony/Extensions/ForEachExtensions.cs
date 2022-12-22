namespace Synchrony.Extensions;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static class ForEachExtensions
{
    internal static void ForEach(this IEnumerable<IObserver<TransactionContext>> subscribers, Action<IObserver<TransactionContext>> action)
    {
        Span<IObserver<TransactionContext>> frames = CollectionsMarshal.AsSpan(subscribers.ToList());
        ref var ptr = ref MemoryMarshal.GetReference(frames);

        for (int i = 0; i < frames.Length; i++)
            action(Unsafe.Add(ref ptr, i));
    }
}