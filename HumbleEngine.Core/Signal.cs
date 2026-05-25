namespace HumbleEngine.Core;

public class Signal<TDelegate>
    where TDelegate : Delegate
{
    public Signal(in Action<TDelegate> callbackHandler, out Action emitter)
    {
        _callbackHandler = callbackHandler;
        emitter = Emit;
    }
    
    private readonly Action<TDelegate> _callbackHandler;

    private readonly List<TDelegate> _connections = [];
    
    public void Connect(TDelegate callback)
        =>  _connections.Add(callback);

    public void Disconnect(TDelegate callback)
        => _connections.Remove(callback);


    private void Emit()
    {
        foreach (var connection in _connections)
            _callbackHandler(connection);
    }
}

public class Signal(out Action emitter) : Signal<Action>(e => e(), out emitter)
{
    
}