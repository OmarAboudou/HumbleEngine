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
