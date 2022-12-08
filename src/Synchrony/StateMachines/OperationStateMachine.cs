namespace Synchrony.StateMachines;

using MassTransit;
using Events;
using Sagas;
using Activities;

public class OperationStateMachine :
    MassTransitStateMachine<OperationState>
{
    public State Completed { get; }
    public State Failed { get; }
    public State Compensated { get; }
    public State Pending { get; }
    
    public Event<RequestExecuteOperation> ExecuteOperationRequest { get; }
    public Event<OperationCompleted> OperationCompleted { get; }
    public Event<OperationFailed> OperationFailed { get; }
    public Event<RequestCompensation> CompensationRequested { get; }

    public OperationStateMachine()
    {
        ConfigureEvents();
        
        InstanceState(x => x.State, Pending, Completed, Failed, Compensated);

        Initially(
            When(ExecuteOperationRequest)
                .Activity(x => x.OfType<ExecuteOperationActivity>()));

        During(Pending,
            When(OperationCompleted)
                .Activity(x => x.OfType<CompleteOperationActivity>()),
            When(OperationFailed)
                .Activity(x => x.OfType<FailedOperationActivity>()));

        During(Failed,
            When(CompensationRequested)
                .Activity(x => x.OfType<CompensatedOperationActivity>()));

        During(Completed,
            Ignore(ExecuteOperationRequest),
            Ignore(OperationCompleted),
            Ignore(OperationFailed));
    }

    void ConfigureEvents()
    {
        Event(() => ExecuteOperationRequest, e => e.CorrelateById(context => context.Message.OperationId));
        Event(() => OperationCompleted, e => e.CorrelateById(context => context.Message.OperationId));
        Event(() => OperationFailed, e => e.CorrelateById(context => context.Message.OperationId));
        Event(() => CompensationRequested, e => e.CorrelateById(context => context.Message.OperationId));
    }
}