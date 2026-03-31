namespace HumbleEngine.Core;

public readonly record struct SignalConnection<TDelegate>
    where TDelegate : Delegate
{
    public readonly TDelegate Delegate;
    internal SignalConnection(TDelegate @delegate) => Delegate = @delegate;
}