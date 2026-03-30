namespace HumbleEngine.Core;

/// <summary>
/// Represents a named logging channel used to categorize log entries by system or subsystem.
/// </summary>
/// <remarks>
/// Implement this interface to define a custom channel.
/// Channels allow filtering log output per subsystem independently of the global log level.
/// <code>
/// class PhysicsChannel : ILogChannel
/// {
///     public static string ChannelName => "Physics";
/// }
/// </code>
/// </remarks>
public interface ILogChannel
{
    /// <summary>
    /// The display name of this channel, used when formatting log output.
    /// </summary>
    public static abstract string ChannelName { get; }
}