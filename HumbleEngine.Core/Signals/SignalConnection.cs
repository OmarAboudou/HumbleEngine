namespace HumbleEngine.Core;

public record SignalConnection<TDelegate>(TDelegate Delegate)
{
    public static implicit operator TDelegate(SignalConnection<TDelegate> connection) => connection.Delegate;
    public static implicit operator SignalConnection<TDelegate>(TDelegate @delegate) => new(@delegate);
}