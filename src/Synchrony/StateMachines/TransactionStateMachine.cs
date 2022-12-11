namespace Synchrony.StateMachines;

using MassTransit;
using Activities;
using Events;
using Sagas;

public class TransactionStateMachine :
    MassTransitStateMachine<TransactionState>
{
    public State Completed { get; }
    public State Failed { get; }
    public State Compensated { get; }
    public State Pending { get; }
    
    public Event<StartTransaction> StartTransactionRequest { get; }
    public Event<TransactionCompleted> TransactionCompleted { get; }
    public Event<TransactionFailed> TransactionFailed { get; }
    public Event<RequestCompensation> CompensationRequested { get; }
    public Event<OperationFailed> OperationFailed { get; }

    public TransactionStateMachine()
    {
        ConfigureEvents();

        InstanceState(x => x.State, Pending, Completed, Failed, Compensated);

        Initially(
            When(StartTransactionRequest)
                .Activity(x => x.OfType<StartTransactionActivity>()));

        During(Pending,
            When(TransactionCompleted)
                .Activity(x => x.OfType<TransactionCompletedActivity>()),
            When(OperationFailed)
                .Activity(x => x.OfType<TransactionOperationFailedActivity>()));
        // When(TransactionFailed)
        //     .TransitionTo(Failed));

        During(Failed,
            When(CompensationRequested)
                .Activity(x => x.OfType<CompensationRequestedActivity>()));

        During(Completed,
            Ignore(StartTransactionRequest),
            Ignore(TransactionCompleted),
            Ignore(OperationFailed));
        // Ignore(TransactionFailed));

        During(Compensated,
            When(CompensationRequested)
                .Then(x => Console.WriteLine($"Operation Id {x.Message.OperationId} failed")));
            // Ignore(CompensationRequested));
    }

    void ConfigureEvents()
    {
        Event(() => StartTransactionRequest, e => e.CorrelateById(context => context.Message.TransactionId));
        Event(() => TransactionCompleted, e => e.CorrelateById(context => context.Message.TransactionId));
        Event(() => TransactionFailed, e => e.CorrelateById(context => context.Message.TransactionId));
        Event(() => CompensationRequested, e => e.CorrelateById(context => context.Message.TransactionId));
        Event(() => OperationFailed, e => e.CorrelateById(context => context.Message.TransactionId));
    }
}