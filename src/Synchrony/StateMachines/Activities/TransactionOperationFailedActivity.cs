namespace Synchrony.StateMachines.Activities;

using MassTransit;
using Extensions;
using Events;
using Sagas;

public class TransactionOperationFailedActivity :
    IStateMachineActivity<TransactionState, OperationFailed>
{
    private readonly TransactionStateMachine _stateMachine;
    private readonly ITransactionCache _cache;

    public TransactionOperationFailedActivity(TransactionStateMachine stateMachine, ITransactionCache cache)
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

    public async Task Execute(BehaviorContext<TransactionState, OperationFailed> context, IBehavior<TransactionState, OperationFailed> next)
    {
        _cache
            .Get(context.Message.TransactionId)
            .GetSubscribers()
            .SendToSubscribers(
                new()
                {
                    TransactionId = context.Message.TransactionId,
                    State = TransactionStates.Failed
                });

        await context.TransitionToState(_stateMachine.Failed);

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(BehaviorExceptionContext<TransactionState, OperationFailed, TException> context,
        IBehavior<TransactionState, OperationFailed> next) where TException : Exception
    {
        throw new NotImplementedException();
    }
}