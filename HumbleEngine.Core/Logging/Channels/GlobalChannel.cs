namespace HumbleEngine.Core;

/// <summary>
/// The built-in channel used by HumbleEngine for internal log messages that don't belong to a specific subsystem.
/// </summary>
internal struct GlobalChannel : ILogChannel
{
    public static string ChannelName => "GLOBAL";
}