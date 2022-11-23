namespace Synchrony.StateMachines;

using MassTransit;
using Events;
using Sagas;

public class OperationStateMachine :
    MassTransitStateMachine<OperationState>
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
                .Then(x => Console.WriteLine($"Operation Id {x.CorrelationId} completed"))
                .TransitionTo(Completed),
            When(OperationFailed)
                .Then(x => Console.WriteLine($"Operation Id {x.CorrelationId} failed"))
                .TransitionTo(Failed));

        During(Failed,
            When(CompensationRequested)
                .TransitionTo(Compensated));

        During(Completed,
            Ignore(StartOperationRequest),
            Ignore(OperationCompleted),
            Ignore(OperationFailed));
    }

    bool IsExecutable(BehaviorContext<OperationState,StartOperation> operation)
    {
        if (operation.Message.Name != operation.Saga.Name)
            return true;
        
        switch ((TransactionStates)operation.Saga.State)
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

    void ConfigureEvents()
    {
        Event(() => StartOperationRequest, e => e.CorrelateById(context => context.Message.OperationId));
        Event(() => OperationCompleted, e => e.CorrelateById(context => context.Message.OperationId));
        Event(() => OperationFailed, e => e.CorrelateById(context => context.Message.OperationId));
        Event(() => CompensationRequested, e => e.CorrelateById(context => context.Message.OperationId));
    }
}
