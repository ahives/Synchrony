namespace Synchrony;

using MassTransit;

internal class EmptyOperationBuilder :
    IOperationBuilder
{
    public TransactionOperation Create(Guid transactionId, int sequenceNumber) =>
        new()
        {
            TransactionId = transactionId,
            OperationId = NewId.NextGuid(),
            SequenceNumber = -1,
            Work = () => false,
            Compensation = () => { }
        };
}