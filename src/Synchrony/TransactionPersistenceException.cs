using System.Runtime.Serialization;

namespace Synchrony;

public class TransactionPersistenceException :
    Exception
{
    public TransactionPersistenceException()
    {
    }

    protected TransactionPersistenceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public TransactionPersistenceException(string? message) : base(message)
    {
    }

    public TransactionPersistenceException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}