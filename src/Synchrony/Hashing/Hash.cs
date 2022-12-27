namespace Synchrony.Hashing;

using System.Security.Cryptography;
using System.Text;

public static class Hash
{
    public static string SHA512Algorithm(string source)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        string base64String = Convert.ToBase64String(bytes);
        Span<byte> buffer = stackalloc byte[bytes.Length];

        if (!Convert.TryFromBase64String(base64String, buffer, out _))
            return null!;

        Span<byte> destination = stackalloc byte[(buffer.Length * 6) >> 3];
        
        return SHA512.Create().TryComputeHash(buffer, destination, out _) ? Convert.ToBase64String(destination) : null!;
    }
}