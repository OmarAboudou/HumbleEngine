namespace HumbleEngine.Core;

public abstract class EmittableSignalBase<TDelegate, TSignal> : ISignalBase<TDelegate, TSignal>
    where TDelegate : Delegate
    where TSignal : ISignalBase<TDelegate, TSignal>
{
    public object Owner 
        => Signal.Owner;
    public string Name 
        => Signal.Name;
    public IReadOnlyList<(Type type, string name)> Parameters 
        => Signal.Parameters;
    
    internal readonly TSignal Signal;
    internal EmittableSignalBase(TSignal signal) 
        => Signal = signal;
    public SignalConnection<TDelegate> Connect(TDelegate @delegate)
        => Signal.Connect(@delegate);

    public void Disconnect(SignalConnection<TDelegate> connection)
        => Signal.Disconnect(connection);

    public void Disconnect(TDelegate @delegate)
        => Signal.Disconnect(@delegate);
}

public class EmittableSignal : EmittableSignalBase<Action, Signal>, ISignal<EmittableSignal>
{
    internal EmittableSignal(Signal signal) : base(signal){}

    public void Emit()
        => Signal.Connections.ToHashSet().ForEach(s => s.Delegate.Invoke());
}

public class EmittableSignal<T1> : EmittableSignalBase<Action<T1>, Signal<T1>>, ISignal<EmittableSignal<T1>, T1>
{
    internal EmittableSignal(Signal<T1> signal) : base(signal) { }

    public void Emit(T1 arg1)
        => Signal.Connections.ToHashSet().ForEach(s => s.Delegate.Invoke(arg1));

}

public class EmittableSignal<T1, T2> : EmittableSignalBase<Action<T1, T2>, Signal<T1, T2>>, ISignal<EmittableSignal<T1, T2>, T1, T2>
{
    internal EmittableSignal(Signal<T1, T2> signal) : base(signal) { }

    public void Emit(T1 arg1, T2 arg2)
        => Signal.Connections.ToHashSet().ForEach(s => s.Delegate.Invoke(arg1, arg2));

}

public class EmittableSignal<T1, T2, T3> : EmittableSignalBase<Action<T1, T2, T3>, Signal<T1, T2, T3>>, ISignal<EmittableSignal<T1, T2, T3>, T1, T2, T3>
{
    internal EmittableSignal(Signal<T1, T2, T3> signal) : base(signal) { }

    public void Emit(T1 arg1, T2 arg2, T3 arg3)
        => Signal.Connections.ToHashSet().ForEach(s => s.Delegate.Invoke(arg1, arg2, arg3));

}

public class EmittableSignal<T1, T2, T3, T4> : EmittableSignalBase<Action<T1, T2, T3, T4>, Signal<T1, T2, T3, T4>>, ISignal<EmittableSignal<T1, T2, T3, T4>, T1, T2, T3, T4>
{
    internal EmittableSignal(Signal<T1, T2, T3, T4> signal) : base(signal) { }

    public void Emit(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        => Signal.Connections.ToHashSet().ForEach(s => s.Delegate.Invoke(arg1, arg2, arg3, arg4));

}

