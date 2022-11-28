using Microsoft.Extensions.Caching.Memory;

namespace Synchrony;

public class TransactionCache :
    ITransactionCache
{
    private MemoryCache _cache;

    public TransactionCache()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public void Store(ITransaction transaction)
    {
        Guid transactionId = transaction.GetTransactionId();
        _cache.Set(transactionId, transaction);
    }

    public ITransaction Get(Guid transactionId)
    {
        _cache.TryGetValue(transactionId, out var transaction);

        return (transaction as ITransaction)!;
    }
}

public interface ITransactionCache
{
    void Store(ITransaction transaction);

    ITransaction Get(Guid transactionId);
}
