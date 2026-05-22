namespace HumbleEngine;

public interface IReactiveProperty<T> : IReadOnlyReactiveProperty<T>
{
    public new T Value
    {
        get;
        set;
    }
}