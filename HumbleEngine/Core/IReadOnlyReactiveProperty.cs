namespace HumbleEngine;

public interface IReadOnlyReactiveProperty<out T> : ISignal<T>
{
    public T Value { get; }
}