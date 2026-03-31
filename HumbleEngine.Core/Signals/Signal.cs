using System.Reflection;
using System.Runtime.CompilerServices;

namespace HumbleEngine.Core;

public class Signal<TDelegate>
    where TDelegate : Delegate
{
    public readonly object Owner;
    public readonly string Name;
    private readonly SignalParameterDefinition[] _parameters;
    public IReadOnlyList<SignalParameterDefinition> Parameters => _parameters.AsReadOnly();
    internal readonly HashSet<SignalConnection<TDelegate>> Connections = [];
    
    internal Signal(object owner, string name, params (Type, string?)[] parameters)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ArgumentNullException.ThrowIfNull(parameters);

        _parameters = parameters.Select((param, index) => (SignalParameterDefinition)(param.Item1, param.Item2 ?? $"arg{index}") ).ToArray();
    }

    public static Signal<TDelegate> operator +(Signal<TDelegate> signal, TDelegate @delegate)
    {
        signal.Connect(@delegate);
        return signal;
    }
    public static Signal<TDelegate> operator -(Signal<TDelegate> signal, TDelegate @delegate)
    {
        signal.Disconnect(@delegate);
        return signal;
    }
    
    public SignalConnection<TDelegate> Connect(TDelegate @delegate)
    {
        SignalConnection<TDelegate> newConnection = new(@delegate);
        if (Connections.TryGetValue(newConnection, out SignalConnection<TDelegate> connection))
        {
            Services.Logger.Warning<SignalingChannel>($"Could not connect {connection} to {this} because it is already connected.");
            return connection;
        }

        Connections.Add(newConnection);
        Services.Logger.Trace<SignalingChannel>($"Connected {newConnection} from {this}.");
        return newConnection;
    }
    public void Disconnect(SignalConnection<TDelegate> connection)
    {
        if (!Connections.TryGetValue(connection, out SignalConnection<TDelegate> conn))
        {
            Services.Logger.Warning<SignalingChannel>($"Could not disconnect {connection} from {this} because it is not connected.");
            return;
        }
        Connections.Remove(conn);
        Services.Logger.Trace<SignalingChannel>($"Disconnected {conn} from {this}.");
    }
    public void Disconnect(TDelegate @delegate) => Disconnect(new SignalConnection<TDelegate>(@delegate));

    public override string ToString() => $"Signal : {Owner}.{Name}({String.Join(", ",_parameters)})";
}
