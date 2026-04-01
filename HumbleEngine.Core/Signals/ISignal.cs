namespace HumbleEngine.Core;

public interface ISignalBase<TDelegate, TSelf>
    where TDelegate : Delegate
    where TSelf : ISignalBase<TDelegate, TSelf> 
{
    public object Owner { get; }
    public string Name { get; }
    
    public IReadOnlyList<(Type type, string name)> Parameters { get; }

    /// <summary>Connects <paramref name="delegate"/> to this signal. Equivalent to <see cref="Connect"/>.</summary>
    /// <remarks>Only supports named methods. For lambdas, use <see cref="Connect"/> and store the returned <see cref="SignalConnection{TDelegate}"/>.</remarks>
    public static TSelf operator +(ISignalBase<TDelegate, TSelf> signal, TDelegate @delegate)
    {
        signal.Connect(@delegate);
        return (TSelf)signal;
    }

    /// <summary>Disconnects <paramref name="delegate"/> from this signal. Equivalent to <see cref="Disconnect(TDelegate)"/>.</summary>
    /// <remarks>Only supports named methods. For lambdas, use <see cref="Disconnect(SignalConnection{TDelegate})"/> with a stored connection.</remarks>
    public static TSelf operator -(ISignalBase<TDelegate, TSelf> signal, TDelegate @delegate)
    {
        signal.Disconnect(@delegate);
        return (TSelf)signal;
    }

    public SignalConnection<TDelegate> Connect(TDelegate @delegate);
    public void Disconnect(SignalConnection<TDelegate> connection);
    public sealed void Disconnect(TDelegate @delegate) => Disconnect(new SignalConnection<TDelegate>(@delegate));
    
}

public interface ISignal<TSelf> : ISignalBase<Action, TSelf>
    where TSelf : ISignal<TSelf> { }
public interface ISignal<TSelf, T1> : ISignalBase<Action<T1>, TSelf>
    where TSelf : ISignal<TSelf, T1> { }
public interface ISignal<TSelf, T1, T2> : ISignalBase<Action<T1, T2>, TSelf>
    where TSelf : ISignal<TSelf, T1, T2> { }
public interface ISignal<TSelf, T1, T2, T3> : ISignalBase<Action<T1, T2, T3>, TSelf>
    where TSelf : ISignal<TSelf, T1, T2, T3> { }
public interface ISignal<TSelf, T1, T2, T3, T4> : ISignalBase<Action<T1, T2, T3, T4>, TSelf>
    where TSelf : ISignal<TSelf, T1, T2, T3, T4> { }