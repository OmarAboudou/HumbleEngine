namespace HumbleEngine;

public interface IReadOnlyReactiveProperty<T> : ISignal<T>
{
    public T Value { get; }
}