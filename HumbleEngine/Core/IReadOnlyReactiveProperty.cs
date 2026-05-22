namespace HumbleEngine;

public interface IReadOnlyReactiveProperty<out T> : ISignal<T>
{
    public ISignal<T, T> Reaffected { get; }
    public T Value { get; }
}