namespace Synchrony;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public abstract class ObservableTransaction :
    IObservable<TransactionContext>
{
    protected readonly List<IObserver<TransactionContext>> _subscribers;

    protected ObservableTransaction()
    {
        _subscribers = new List<IObserver<TransactionContext>>();
    }

    public IDisposable Subscribe(IObserver<TransactionContext> observer)
    {
        if (!_subscribers.Contains(observer))
            _subscribers.Add(observer);

        return new UnSubscriber<TransactionContext>(_subscribers, observer);
    }

    protected virtual void StopSendingNotifications()
    {
        Span<IObserver<TransactionContext>> memory = CollectionsMarshal.AsSpan(_subscribers);
        ref var ptr = ref MemoryMarshal.GetReference(memory);

        for (int i = 0; i < memory.Length; i++)
        {
            var subscriber = Unsafe.Add(ref ptr, i);
            subscriber.OnCompleted();
        }

        _subscribers.Clear();
    }

    
    class UnSubscriber<T> :
        IDisposable
    {
        private readonly List<IObserver<T>> _subscribers;
        private readonly IObserver<T> _subscriber;

        public UnSubscriber(List<IObserver<T>> subscribers, IObserver<T> subscriber)
        {
            _subscribers = subscribers;
            _subscriber = subscriber;
        }

        public void Dispose()
        {
            if (_subscribers.Contains(_subscriber))
                _subscribers.Remove(_subscriber);
        }
    }
}