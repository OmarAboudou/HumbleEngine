namespace HumbleEngine;

public interface IAnimation
{
    bool IsComplete { get; }
    void Advance(double delta);
}

public interface IAnimation<T> : IAnimation
{
    T Value { get; }
}
