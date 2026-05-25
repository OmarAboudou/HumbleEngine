namespace HumbleEngine.Core;

public class Signal : IDisposable
{
    public Signal(out Action emitter)
    {
        emitter = Emit;
    }

    private readonly List<Action> _connections = [];
    
    public void Connect(Action signal)
        =>  _connections.Add(signal);

    public void Disconnect(Action signal)
        => _connections.Remove(signal);

    private void Emit()
    {
        foreach (var connection in _connections) 
            connection();
    }
    
    public void Dispose()
    {
        // TODO release managed resources here
    }
}