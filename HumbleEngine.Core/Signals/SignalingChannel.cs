namespace HumbleEngine.Core;

/// <summary>
/// The built-in channel used by HumbleEngine for internal signal system log messages.
/// </summary>
internal struct SignalingChannel : ILogChannel
{
    public static string ChannelName => "SIGNALING";
}