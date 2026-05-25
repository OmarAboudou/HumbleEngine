namespace HumbleEngine.Core;

public class Signal<TDelegate>
    where TDelegate : Delegate
{
    private readonly Action<TDelegate> _callbackHandler;
    
    public Signal(in Action<TDelegate> callbackHandler, out Action emitter)
    {
        _callbackHandler = callbackHandler;
        emitter = Emit;
    }

    private readonly List<TDelegate> _connections = [];
    public static Signal<TDelegate> operator +(Signal<TDelegate> signal, TDelegate callback)
    {
        signal.Connect(callback);
        return signal;
    }
    
    public static Signal<TDelegate> operator -(Signal<TDelegate> signal, TDelegate callback)
    {
        signal.Disconnect(callback);
        return signal;
    }
    
    private void Connect(TDelegate callback)
        =>  _connections.Add(callback);

    private void Disconnect(TDelegate callback)
        => _connections.Remove(callback);
    
    
    private void Emit()
    {
        foreach (var connection in _connections)
            _callbackHandler(connection);
    }
}

public class Signal : Signal<Action>
{
    public Signal(out Action emitter) : base((e) => e(), out Action _emitter)
    {
        emitter = _emitter;
    }
}