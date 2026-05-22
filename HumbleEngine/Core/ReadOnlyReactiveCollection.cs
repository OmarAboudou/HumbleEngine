namespace HumbleEngine;

public class ReadOnlyReactiveCollection<T> : IReadOnlyReactiveCollection<T>
{
    private IReadOnlyReactiveCollection<T> _reactiveCollection;

    public ReadOnlyReactiveCollection(IReactiveCollection<T> reactiveCollection)
    {
        _reactiveCollection = reactiveCollection;
    }

    public void Connect(Action<IReadOnlyList<T>> listener)
    {
        _reactiveCollection.Connect(listener);
    }

    public void Disconnect(Action<IReadOnlyList<T>> listener)
    {
        _reactiveCollection.Disconnect(listener);
    }

    public ISignal<IReadOnlyList<T>, IReadOnlyList<T>> Reaffected => _reactiveCollection.Reaffected;

    public IReadOnlyList<T> Value => _reactiveCollection.Value;

    public ISignal<int, T> ItemAdded => _reactiveCollection.ItemAdded;

    public ISignal<int, T> ItemRemoved => _reactiveCollection.ItemRemoved;

    public ISignal Cleared => _reactiveCollection.Cleared;

    public ISignal Changed => _reactiveCollection.Changed;
}