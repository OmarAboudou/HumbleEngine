namespace HumbleEngine;

public class Signal : IReadOnlySignal
{
    internal readonly List<Action> Connections = [];
    
    public void Connect(Action callback) => Connections.Add(callback);
    public void Disconnect(Action callback) => Connections.Remove(callback);
    
    public void Emit() => Connections.ForEach(connection => connection.Invoke());
    
    private ReadOnlySignal? _readOnlySignal;
    public ReadOnlySignal AsReadOnly() => _readOnlySignal ??= new ReadOnlySignal(this);
}

public class Signal<T> : IReadOnlySignal<T>
{
    internal readonly List<Action<T>> Connections = [];
    
    public void Connect(Action<T> callback) => Connections.Add(callback);
    public void Disconnect(Action<T> callback) => Connections.Remove(callback);
    
    public void Emit(T arg) => Connections.ForEach(connection => connection.Invoke(arg));
    
    private ReadOnlySignal<T>? _readOnlySignal;
    public ReadOnlySignal<T> AsReadOnly() => _readOnlySignal ??= new ReadOnlySignal<T>(this);

}

public class Signal<T1, T2> : IReadOnlySignal<T1, T2>
{
    internal readonly List<Action<T1, T2>> Connections = [];
    
    public void Connect(Action<T1, T2> callback) => Connections.Add(callback);
    public void Disconnect(Action<T1, T2> callback) => Connections.Remove(callback);
    
    public void Emit(T1 arg1, T2 arg2) => Connections.ForEach(connection => connection.Invoke(arg1,arg2));
    
    private ReadOnlySignal<T1, T2>? _readOnlySignal;
    public ReadOnlySignal<T1, T2> AsReadOnly() => _readOnlySignal ??= new ReadOnlySignal<T1, T2>(this);

}

// -----------------------------------------------------------

public class ReadOnlySignal : IReadOnlySignal
{
    private readonly Signal _signal;
    
    internal ReadOnlySignal(Signal signal) => _signal = signal;
    
    public void Connect(Action callback) => _signal.Connect(callback);
    public void Disconnect(Action callback) => _signal.Disconnect(callback);
}

public class ReadOnlySignal<T> : IReadOnlySignal<T>
{
    private readonly Signal<T> _signal;
    
    internal ReadOnlySignal(Signal<T> signal) => _signal = signal;
    
    public void Connect(Action<T> callback) => _signal.Connect(callback);
    public void Disconnect(Action<T> callback) => _signal.Disconnect(callback);
}

public class ReadOnlySignal<T1, T2> : IReadOnlySignal<T1, T2>
{
    private readonly Signal<T1, T2> _signal;
    
    internal ReadOnlySignal(Signal<T1, T2> signal) => _signal = signal;
    
    public void Connect(Action<T1, T2> callback) => _signal.Connect(callback);
    public void Disconnect(Action<T1, T2> callback) => _signal.Disconnect(callback);
}

// -----------------------------------------------------------

public interface IReadOnlySignal
{
    public void Connect(Action callback);
    public void Disconnect(Action callback);
}

public interface IReadOnlySignal<out T>
{
    public void Connect(Action<T> callback);
    public void Disconnect(Action<T> callback);
}

public interface IReadOnlySignal<out T1, out T2>
{
    public void Connect(Action<T1, T2> callback);
    public void Disconnect(Action<T1, T2> callback);
}