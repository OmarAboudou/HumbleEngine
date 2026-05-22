namespace HumbleEngine;

public interface IReadOnlyReactiveCollection<out T> : IReadOnlyReactiveProperty<IReadOnlyList<T>>
{
    public ISignal<int, T> ItemAdded { get; }
    public ISignal<int, T> ItemRemoved{ get; }
    public ISignal                      Cleared   { get; }
    public ISignal                      Changed   { get; }
}