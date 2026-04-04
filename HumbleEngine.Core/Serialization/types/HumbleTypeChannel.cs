namespace HumbleEngine.Core;

/// <summary>
/// Canal de log dédié aux événements du système de types sérialisables.
/// </summary>
public struct HumbleTypeChannel : ILogChannel
{
    public static string ChannelName { get; } =  "HUMBLE TYPE";
}