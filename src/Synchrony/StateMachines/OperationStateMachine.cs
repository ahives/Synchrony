namespace Synchrony.StateMachines;

using MassTransit;
using Events;
using Sagas;

public class OperationStateMachine :
    MassTransitStateMachine<OperationState2>
{
    public State Completed { get; }
    public State Failed { get; }
    public State Compensated { get; }
    public State Pending { get; }
    
    public Event<StartOperation> StartOperationRequest { get; }
    public Event<OperationCompleted> OperationCompleted { get; }
    public Event<OperationFailed> OperationFailed { get; }
    public Event<RequestCompensation> CompensationRequested { get; }

    public OperationStateMachine()
    {
        ConfigureEvents();
        
        InstanceState(x => x.State, Pending, Completed, Failed, Compensated);

        Initially(
            When(StartOperationRequest)
                // .Activity(x => x.OfType<InitActivity>())
                .If(IsExecutable, y => y.TransitionTo(Pending)));
                // .TransitionTo(Pending));

        During(Pending,
            When(OperationCompleted)
                // .Then(x => Console.WriteLine($"Operation Id {x.CorrelationId} completed"))
                .TransitionTo(Completed),
            When(OperationFailed)
                .TransitionTo(Failed));

        During(Failed,
            When(CompensationRequested)
                .TransitionTo(Compensated));

        During(Completed,
            Ignore(StartOperationRequest),
            Ignore(OperationCompleted),
            Ignore(OperationFailed));
    }

    bool IsExecutable(BehaviorContext<OperationState2,StartOperation> operation)
    {
        if (operation.Message.Name != operation.Saga.Name)
            return true;
        
        switch ((OperationState)operation.Saga.State)
        {
            case OperationState.New:
            case OperationState.Pending:
                return true;
            case OperationState.Failed:
            case OperationState.Completed:
            case OperationState.Compensated:
            default:
                return false;
        }
    }

    void ConfigureEvents()
    {
        Event(() => StartOperationRequest, e => e.CorrelateById(context => context.Message.OperationId));
        Event(() => OperationCompleted, e => e.CorrelateById(context => context.Message.OperationId));
        Event(() => OperationFailed, e => e.CorrelateById(context => context.Message.OperationId));
        Event(() => CompensationRequested, e => e.CorrelateById(context => context.Message.OperationId));
    }
}

public class InitActivity :
    IStateMachineActivity<OperationState2, StartOperation>
{
    public void Probe(ProbeContext context)
    {
        throw new NotImplementedException();
    }

    public void Accept(StateMachineVisitor visitor)
    {
        throw new NotImplementedException();
    }

    public async Task Execute(BehaviorContext<OperationState2, StartOperation> context, IBehavior<OperationState2, StartOperation> next)
    {
        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(BehaviorExceptionContext<OperationState2, StartOperation, TException> context, IBehavior<OperationState2, StartOperation> next) where TException : Exception
    {
        throw new NotImplementedException();
    }
}