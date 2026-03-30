namespace HumbleEngine.Core;

/// <summary>
/// Provides shared, application-wide instances of core HumbleEngine services.
/// </summary>
public static class Services
{
    /// <summary>
    /// The shared <see cref="Logger"/> instance used throughout the engine.
    /// </summary>
    public static Logger Logger { get; } = new();
}