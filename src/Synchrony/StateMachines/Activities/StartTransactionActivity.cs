namespace Synchrony.StateMachines.Activities;

using MassTransit;
using Extensions;
using Events;
using Sagas;

public class StartTransactionActivity :
    IStateMachineActivity<TransactionState, StartTransaction>
{
    private readonly TransactionStateMachine _stateMachine;
    private readonly ITransactionCache _cache;

    public StartTransactionActivity(TransactionStateMachine stateMachine, ITransactionCache cache)
    {
        _stateMachine = stateMachine;
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

    public async Task Execute(BehaviorContext<TransactionState, StartTransaction> context, IBehavior<TransactionState, StartTransaction> next)
    {
        _cache
            .Get(context.Message.TransactionId)
            .GetSubscribers()
            .SendToSubscribers(
                new()
                {
                    TransactionId = context.Message.TransactionId,
                    State = TransactionStates.Pending
                });

        await context.TransitionToState(_stateMachine.Pending);

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(BehaviorExceptionContext<TransactionState, StartTransaction, TException> context,
        IBehavior<TransactionState, StartTransaction> next) where TException : Exception
    {
        throw new NotImplementedException();
    }
}