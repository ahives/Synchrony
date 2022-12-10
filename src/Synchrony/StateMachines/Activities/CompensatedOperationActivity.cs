namespace Synchrony.StateMachines.Activities;

using MassTransit;
using Extensions;
using Events;
using Sagas;

public class CompensatedOperationActivity :
    IStateMachineActivity<OperationState, RequestCompensation>
{
    private readonly OperationStateMachine _stateMachine;
    private readonly ITransactionCache _cache;

    public CompensatedOperationActivity(OperationStateMachine stateMachine, ITransactionCache cache)
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

    public async Task Execute(BehaviorContext<OperationState, RequestCompensation> context, IBehavior<OperationState, RequestCompensation> next)
    {
        _cache
            .Get(context.Message.TransactionId)
            .GetSubscribers()
            .SendToSubscribers(
                new()
                {
                    TransactionId = context.Message.TransactionId,
                    OperationId = context.Message.OperationId,
                    State = TransactionStates.Compensated
                });

        await context.TransitionToState(_stateMachine.Compensated);

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(BehaviorExceptionContext<OperationState, RequestCompensation, TException> context,
        IBehavior<OperationState, RequestCompensation> next) where TException : Exception
    {
        throw new NotImplementedException();
    }
}