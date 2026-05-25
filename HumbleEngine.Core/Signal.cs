namespace HumbleEngine.Core;

public abstract class SignalBase : IDisposable
{
    public abstract void Dispose();
}

public class Signal : SignalBase
{
    internal Signal(out Action emitter)
        => emitter = Emit;

    private readonly List<WeakReference<Action>> _connections = [];

    public void Connect(Action callback)
    {
        CleanConnections();
        
        _connections.Add(new WeakReference<Action>(callback));
    }

    public void Disconnect(Action callback)
    {
        CleanConnections();
        
        _connections.Add(new WeakReference<Action>(callback));
    }

    private void CleanConnections()
    {
        _connections.RemoveAll(wr => !wr.TryGetTarget(out _));
    }
    
    private void Emit()
    {
        CleanConnections();
        
        _connections.RemoveAll(wr => !wr.TryGetTarget(out _));
        
        foreach (WeakReference<Action> connection in _connections) 
            if(connection.TryGetTarget(out var callback))
                callback();
    }

    public override void Dispose()
    {
        _connections.Clear();
    }
}

public class Signal<T> : SignalBase
{
    internal Signal(string arg1Name, out Action<T> emitter)
    {
        Arg1Name = arg1Name;
        emitter = Emit;
    }

    public readonly string Arg1Name;

    private readonly List<WeakReference<Action<T>>> _connections = [];

    public void Connect(Action<T> callback)
    {
        CleanConnections();
        
        _connections.Add(new WeakReference<Action<T>>(callback));
    }

    public void Disconnect(Action<T> callback)
    {
        CleanConnections();
        
        _connections.Add(new WeakReference<Action<T>>(callback));
    }

    private void CleanConnections()
    {
        _connections.RemoveAll(wr => !wr.TryGetTarget(out _));
    }
    
    private void Emit(T arg)
    {
        CleanConnections();
        
        foreach (WeakReference<Action<T>> connection in _connections) 
            if(connection.TryGetTarget(out var callback))
                callback(arg);
    }

    public override void Dispose()
    {
        _connections.Clear();
    }
}

public class Signal<T1, T2> : SignalBase
{
    internal Signal(string arg1Name, string arg2Name, out Action<T1, T2> emitter)
    {
        Arg1Name = arg1Name;
        Arg2Name  = arg2Name;
        emitter = Emit;
    }

    public readonly string Arg1Name;
    public readonly string Arg2Name;

    private readonly List<WeakReference<Action<T1, T2>>> _connections = [];

    public void Connect(Action<T1, T2> callback)
    {
        CleanConnections();
        
        _connections.Add(new WeakReference<Action<T1, T2>>(callback));
    }

    public void Disconnect(Action<T1, T2> callback)
    {
        CleanConnections();
        
        _connections.Add(new WeakReference<Action<T1, T2>>(callback));
    }

    private void CleanConnections()
    {
        _connections.RemoveAll(wr => !wr.TryGetTarget(out _));
    }
    
    private void Emit(T1 arg1, T2 arg2)
    {
        CleanConnections();
        
        _connections.RemoveAll(wr => !wr.TryGetTarget(out _));
        
        foreach (WeakReference<Action<T1, T2>> connection in _connections) 
            if(connection.TryGetTarget(out var callback))
                callback(arg1, arg2);
    }

    public override void Dispose()
    {
        _connections.Clear();
    }
}