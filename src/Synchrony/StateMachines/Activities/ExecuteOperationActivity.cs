namespace Synchrony.StateMachines.Activities;

using MassTransit;
using Extensions;
using Events;
using Sagas;

public class ExecuteOperationActivity :
    IStateMachineActivity<OperationState, RequestExecuteOperation>
{
    private readonly OperationStateMachine _stateMachine;
    private readonly ITransactionCache _cache;

    public ExecuteOperationActivity(OperationStateMachine stateMachine, ITransactionCache cache)
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

    public async Task Execute(BehaviorContext<OperationState, RequestExecuteOperation> context, IBehavior<OperationState, RequestExecuteOperation> next)
    {
        if (IsExecutable(context))
        {
            _cache
                .Get(context.Message.TransactionId)
                .GetSubscribers()
                .SendToSubscribers(
                    new()
                    {
                        TransactionId = context.Message.TransactionId,
                        OperationId = context.Message.OperationId,
                        State = TransactionStates.New
                    });

            await context.TransitionToState(_stateMachine.Pending);
        }

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OperationState, RequestExecuteOperation, TException> context,
        IBehavior<OperationState, RequestExecuteOperation> next) where TException : Exception
    {
        throw new NotImplementedException();
    }

    bool IsExecutable(BehaviorContext<OperationState, RequestExecuteOperation> context)
    {
        if (context.Message.Name != context.Saga.Name)
            return true;
        
        switch ((TransactionStates)context.Saga.State)
        {
            case TransactionStates.New:
            case TransactionStates.Pending:
                return true;
            case TransactionStates.Failed:
            case TransactionStates.Completed:
            case TransactionStates.Compensated:
            default:
                return false;
        }
    }
}