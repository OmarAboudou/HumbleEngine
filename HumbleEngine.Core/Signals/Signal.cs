namespace HumbleEngine.Core;

public class SignalBase<TDelegate, TSelf>
    where TDelegate : Delegate
    where TSelf : SignalBase<TDelegate, TSelf> 
{
    public readonly object Owner;
    public readonly string Name;
    private readonly SignalParameterDefinition[] _parameters;
    public IReadOnlyList<SignalParameterDefinition> Parameters => _parameters.AsReadOnly();
    internal readonly HashSet<SignalConnection<TDelegate>> Connections = [];
    
    internal SignalBase(object owner, string name, params (Type, string?)[] parameters)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ArgumentNullException.ThrowIfNull(parameters);

        _parameters = parameters.Select((param, index) => (SignalParameterDefinition)(param.Item1, param.Item2 ?? $"arg{index}") ).ToArray();
    }

    public static TSelf operator +(SignalBase<TDelegate, TSelf> signal, TDelegate @delegate)
    {
        signal.Connect(@delegate);
        return (TSelf)signal;
    }
    public static TSelf operator -(SignalBase<TDelegate, TSelf> signal, TDelegate @delegate)
    {
        signal.Disconnect(@delegate);
        return (TSelf)signal;
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

public class Signal : SignalBase<Action, Signal>
{
    internal Signal(object owner, string name, params (Type, string?)[] parameters) : base(owner, name, parameters){}
}

public class Signal<T1> : SignalBase<Action<T1>, Signal<T1>>
{
    internal Signal(object owner, string name, params (Type, string?)[] parameters) : base(owner, name, parameters){}
}

public class Signal<T1, T2> : SignalBase<Action<T1, T2>, Signal<T1, T2>>
{
    internal Signal(object owner, string name, params (Type, string?)[] parameters) : base(owner, name, parameters){}
}

public class Signal<T1, T2, T3> : SignalBase<Action<T1, T2, T3>, Signal<T1, T2, T3>>
{
    internal Signal(object owner, string name, params (Type, string?)[] parameters) : base(owner, name, parameters){}
}

public class Signal<T1, T2, T3, T4> : SignalBase<Action<T1, T2, T3, T4>, Signal<T1, T2, T3, T4>>
{
    internal Signal(object owner, string name, params (Type, string?)[] parameters) : base(owner, name, parameters){}
}