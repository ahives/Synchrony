namespace Synchrony.Extensions;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Diagnostics;

public static class ComputeHashExtensions
{
    public static string ComputeHash(this List<IOperation> operations, Func<string, string> hashAlgorithm)
    {
        Guard.IsNotNull(operations);

        if (operations.Count == 0)
            return null!;
        
        Span<IOperation> memory = CollectionsMarshal.AsSpan(operations);
        ref var ptr = ref MemoryMarshal.GetReference(memory);
        StringBuilder buffer = new StringBuilder();

        for (int i = 0; i < memory.Length; i++)
        {
            var operation = Unsafe.Add(ref ptr, i);
            if (i == 0)
            {
                buffer.Append(operation.Metadata.Name);
                continue;
            }

            buffer.Append($"|{operation.Metadata.Name}");
        }

        return hashAlgorithm(buffer.ToString());
    }
}