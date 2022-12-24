namespace Synchrony.StateMachines.Activities;

using MassTransit;
using Extensions;
using Events;
using Sagas;

public class TransactionCompensatedActivity :
    IStateMachineActivity<TransactionState, RequestCompensation>
{
    private readonly ITransactionCache _cache;

    public TransactionCompensatedActivity(ITransactionCache cache)
    {
        _cache = cache;
    }

    public void Probe(ProbeContext context)
    {
        throw new NotImplementedException();
    }

    public void Accept(StateMachineVisitor visitor)
    {
        throw new NotImplementedException();
    }

    public async Task Execute(BehaviorContext<TransactionState, RequestCompensation> context, IBehavior<TransactionState, RequestCompensation> next)
    {
        _cache
            .Get(context.Message.TransactionId)
            .GetSubscribers()
            .SendToSubscribers(
                new()
                {
                    TransactionId = context.Message.TransactionId,
                    State = TransactionStates.Compensated
                });

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(BehaviorExceptionContext<TransactionState, RequestCompensation, TException> context,
        IBehavior<TransactionState, RequestCompensation> next) where TException : Exception
    {
        throw new NotImplementedException();
    }
}