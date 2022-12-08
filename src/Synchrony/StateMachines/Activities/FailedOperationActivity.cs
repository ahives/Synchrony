namespace Synchrony.StateMachines.Activities;

using MassTransit;
using Extensions;
using Events;
using Sagas;

public class FailedOperationActivity :
    IStateMachineActivity<OperationState, OperationFailed>
{
    private readonly OperationStateMachine _stateMachine;
    private readonly ITransactionCache _cache;

    public FailedOperationActivity(OperationStateMachine stateMachine, ITransactionCache cache)
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

    public async Task Execute(BehaviorContext<OperationState, OperationFailed> context, IBehavior<OperationState, OperationFailed> next)
    {
        _cache
            .Get(context.Message.TransactionId)
            .GetObservers()
            .SendToSubscribers(
                new()
                {
                    TransactionId = context.Message.TransactionId,
                    OperationId = context.Message.OperationId,
                    State = TransactionStates.Failed
                });

        await context.TransitionToState(_stateMachine.Failed);

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(BehaviorExceptionContext<OperationState, OperationFailed, TException> context, IBehavior<OperationState, OperationFailed> next) where TException : Exception
    {
        throw new NotImplementedException();
    }
}