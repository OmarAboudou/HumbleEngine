namespace HumbleEngine.Core;

/// <summary>
/// Represents an active connection between a signal and a delegate.
/// </summary>
/// <remarks>
/// Store the instance returned by <see cref="SignalBase{TDelegate,TSelf}.Connect"/> to disconnect later.
/// Connections are not cleaned up automatically — call <see cref="SignalBase{TDelegate,TSelf}.Disconnect(SignalConnection{TDelegate})"/>
/// when the listener no longer needs to receive the signal.
/// </remarks>
/// <typeparam name="TDelegate">The delegate type of the connected signal.</typeparam>
public readonly record struct SignalConnection<TDelegate>
    where TDelegate : Delegate
{
    /// <summary>The delegate invoked when the signal is emitted.</summary>
    public readonly TDelegate Delegate;
    internal SignalConnection(TDelegate @delegate) => Delegate = @delegate;
}