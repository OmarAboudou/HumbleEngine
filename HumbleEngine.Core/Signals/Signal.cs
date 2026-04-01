using System.Text;

namespace HumbleEngine.Core;

/// <summary>
/// Base class for all signal types. A signal is a typed notification that an object can emit,
/// and to which other objects can subscribe.
/// </summary>
/// <remarks>
/// <para>
/// Signals enforce an ownership model: only the object that created a signal can emit it,
/// via the <c>Emit</c> extension method from <see cref="ExtensionMethods"/>.
/// </para>
/// <para>
/// Connections are not cleaned up automatically. Subscribers are responsible for calling
/// <see cref="Disconnect(SignalConnection{TDelegate})"/> when they no longer need to receive the signal.
/// </para>
/// </remarks>
/// <typeparam name="TDelegate">The delegate type defining the signal's signature.</typeparam>
/// <typeparam name="TSelf">The concrete subtype, used to preserve the return type of operators.</typeparam>
public class SignalBase<TDelegate, TSelf>
    where TDelegate : Delegate
    where TSelf : SignalBase<TDelegate, TSelf>
{
    /// <summary>The object that owns and is allowed to emit this signal.</summary>
    public readonly object Owner;
    /// <summary>The name of this signal, used in logging and tooling.</summary>
    public readonly string Name;
    private readonly Type[] _parameterTypes;
    private readonly string[] _parameterNames;

    /// <summary>The parameter definitions describing the signal's arguments.</summary>
    public (Type type, String name)[] Parameters => _parameterTypes.Zip(_parameterNames).ToArray();

    internal readonly HashSet<SignalConnection<TDelegate>> Connections = [];

    internal SignalBase(object owner, string name, params (Type, string)[] parameters)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ArgumentNullException.ThrowIfNull(parameters);
    
        _parameterTypes = parameters.Select(x => x.Item1).ToArray();
        _parameterNames = parameters.Select(x => x.Item2).ToArray();
    }

    /// <summary>Connects <paramref name="delegate"/> to this signal. Equivalent to <see cref="Connect"/>.</summary>
    /// <remarks>Only supports named methods. For lambdas, use <see cref="Connect"/> and store the returned <see cref="SignalConnection{TDelegate}"/>.</remarks>
    public static TSelf operator +(SignalBase<TDelegate, TSelf> signal, TDelegate @delegate)
    {
        signal.Connect(@delegate);
        return (TSelf)signal;
    }

    /// <summary>Disconnects <paramref name="delegate"/> from this signal. Equivalent to <see cref="Disconnect(TDelegate)"/>.</summary>
    /// <remarks>Only supports named methods. For lambdas, use <see cref="Disconnect(SignalConnection{TDelegate})"/> with a stored connection.</remarks>
    public static TSelf operator -(SignalBase<TDelegate, TSelf> signal, TDelegate @delegate)
    {
        signal.Disconnect(@delegate);
        return (TSelf)signal;
    }

    /// <summary>
    /// Connects a delegate to this signal.
    /// </summary>
    /// <param name="delegate">The delegate to connect.</param>
    /// <returns>
    /// The resulting <see cref="SignalConnection{TDelegate}"/>. Store it to disconnect later.
    /// If the delegate is already connected, the existing connection is returned.
    /// </returns>
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
    /// <summary>
    /// Disconnects a previously established connection from this signal.
    /// </summary>
    /// <param name="connection">The connection to remove.</param>
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
    /// <summary>Disconnects the connection associated with <paramref name="delegate"/>.</summary>
    /// <param name="delegate">The delegate to disconnect.</param>
    public void Disconnect(TDelegate @delegate) => Disconnect(new SignalConnection<TDelegate>(@delegate));

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append("Signal:");
        sb.Append(Owner);
        sb.Append('.');
        sb.Append(Name);
        sb.Append('(');
        var parameters = Parameters;
        parameters.ForEach(p =>
        {
            sb.Append(p.type);
            sb.Append(' ');
            sb.Append(p.name);
            sb.Append(',');
        });
        if (parameters.Length > 0)
        {
            // Remove the last ','
            sb.Remove(sb.Length - 1, 1);
        }
        sb.Append(')');
        return sb.ToString();
    }
}

/// <summary>A signal with no parameters.</summary>
/// <inheritdoc cref="SignalBase{TDelegate,TSelf}"/>
public sealed class Signal : SignalBase<Action, Signal>
{
    internal Signal(object owner, string name, params (Type, string?)[] parameters) : base(owner, name, parameters){}
}

/// <summary>A signal with one parameter.</summary>
/// <inheritdoc cref="SignalBase{TDelegate,TSelf}"/>
/// <typeparam name="T1">The type of the first parameter.</typeparam>
public sealed class Signal<T1> : SignalBase<Action<T1>, Signal<T1>>
{
    internal Signal(object owner, string name, params (Type, string?)[] parameters) : base(owner, name, parameters){}
}

/// <summary>A signal with two parameters.</summary>
/// <inheritdoc cref="SignalBase{TDelegate,TSelf}"/>
/// <typeparam name="T1">The type of the first parameter.</typeparam>
/// <typeparam name="T2">The type of the second parameter.</typeparam>
public sealed class Signal<T1, T2> : SignalBase<Action<T1, T2>, Signal<T1, T2>>
{
    internal Signal(object owner, string name, params (Type, string?)[] parameters) : base(owner, name, parameters){}
}

/// <summary>A signal with three parameters.</summary>
/// <inheritdoc cref="SignalBase{TDelegate,TSelf}"/>
/// <typeparam name="T1">The type of the first parameter.</typeparam>
/// <typeparam name="T2">The type of the second parameter.</typeparam>
/// <typeparam name="T3">The type of the third parameter.</typeparam>
public sealed class Signal<T1, T2, T3> : SignalBase<Action<T1, T2, T3>, Signal<T1, T2, T3>>
{
    internal Signal(object owner, string name, params (Type, string?)[] parameters) : base(owner, name, parameters){}
}

/// <summary>A signal with four parameters.</summary>
/// <inheritdoc cref="SignalBase{TDelegate,TSelf}"/>
/// <typeparam name="T1">The type of the first parameter.</typeparam>
/// <typeparam name="T2">The type of the second parameter.</typeparam>
/// <typeparam name="T3">The type of the third parameter.</typeparam>
/// <typeparam name="T4">The type of the fourth parameter.</typeparam>
public sealed class Signal<T1, T2, T3, T4> : SignalBase<Action<T1, T2, T3, T4>, Signal<T1, T2, T3, T4>>
{
    internal Signal(object owner, string name, params (Type, string)[] parameters) : base(owner, name, parameters){}
}