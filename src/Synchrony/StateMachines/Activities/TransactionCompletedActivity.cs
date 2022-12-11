namespace Synchrony.StateMachines.Activities;

using MassTransit;
using Extensions;
using Events;
using Sagas;

public class TransactionCompletedActivity :
    IStateMachineActivity<TransactionState, TransactionCompleted>
{
    private readonly TransactionStateMachine _stateMachine;
    private readonly ITransactionCache _cache;

    public TransactionCompletedActivity(TransactionStateMachine stateMachine, ITransactionCache cache)
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

    public async Task Execute(BehaviorContext<TransactionState, TransactionCompleted> context, IBehavior<TransactionState, TransactionCompleted> next)
    {
        _cache
            .Get(context.Message.TransactionId)
            .GetSubscribers()
            .SendToSubscribers(
                new()
                {
                    TransactionId = context.Message.TransactionId,
                    State = TransactionStates.Completed
                });

        await context.TransitionToState(_stateMachine.Completed);

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TransactionState, TransactionCompleted, TException> context,
        IBehavior<TransactionState, TransactionCompleted> next) where TException : Exception
    {
        throw new NotImplementedException();
    }
}