namespace HumbleEngine.Core;

public class SignalSource
{
    public Signal<Action> Signal { get; }

    internal SignalSource(Signal<Action> signal) => Signal = signal;

    public void Emit() => Signal.Connections.ForEach(connection => connection.Delegate.Invoke());
}

public class SignalSource<T1>
{
    public Signal<Action<T1>> Signal { get; }
    
    internal SignalSource(Signal<Action<T1>> signal) => Signal = signal;
    
    public void Emit(T1 arg1) => Signal.Connections.ForEach(connection => connection.Delegate.Invoke(arg1));
}

public class SignalSource<T1, T2>
{
    public Signal<Action<T1, T2>> Signal { get; }

    internal SignalSource(Signal<Action<T1, T2>> signal) => Signal = signal;
    
    public void Emit(T1 arg1, T2 arg2) => Signal.Connections.ForEach(connection => connection.Delegate.Invoke(arg1, arg2));
}

public class SignalSource<T1, T2, T3>
{
    public Signal<Action<T1, T2, T3>> Signal { get; }
    
    internal SignalSource(Signal<Action<T1, T2, T3>> signal) => Signal = signal;
    
    public void Emit(T1 arg1, T2 arg2, T3 arg3) => Signal.Connections.ForEach(connection => connection.Delegate.Invoke(arg1, arg2, arg3));
}

public class SignalSource<T1, T2, T3, T4>
{
    public Signal<Action<T1, T2, T3, T4>> Signal { get; }

    internal SignalSource(Signal<Action<T1, T2, T3, T4>> signal) => Signal = signal;
    
    public void Emit(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Signal.Connections.ForEach(connection => connection.Delegate.Invoke(arg1, arg2, arg3, arg4));
}



