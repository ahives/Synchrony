namespace Synchrony.StateMachines.Activities;

using MassTransit;
using Extensions;
using Events;
using Sagas;

public class CompleteOperationActivity :
    IStateMachineActivity<OperationState, OperationCompleted>
{
    private readonly OperationStateMachine _stateMachine;
    private readonly ITransactionCache _cache;

    public CompleteOperationActivity(OperationStateMachine stateMachine, ITransactionCache cache)
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

    public async Task Execute(BehaviorContext<OperationState, OperationCompleted> context, IBehavior<OperationState, OperationCompleted> next)
    {
        _cache
            .Get(context.Message.TransactionId)
            .GetSubscribers()
            .SendToSubscribers(
                new()
                {
                    TransactionId = context.Message.TransactionId,
                    OperationId = context.Message.OperationId,
                    State = TransactionStates.Completed
                });

        await context.TransitionToState(_stateMachine.Completed);

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(BehaviorExceptionContext<OperationState, OperationCompleted, TException> context,
        IBehavior<OperationState, OperationCompleted> next) where TException : Exception
    {
        throw new NotImplementedException();
    }
}