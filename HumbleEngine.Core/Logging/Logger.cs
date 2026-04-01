using System.Diagnostics;

namespace HumbleEngine.Core;

/// <summary>
/// Filters and dispatches log entries to one or more <see cref="ILogSink"/> destinations.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="Logger"/> instance maintains its own set of sinks, a default minimum level,
/// and optional per-channel overrides.
/// </para>
/// <para>
/// For application-wide logging, prefer <see cref="Services.Logger"/> which provides a shared instance.
/// Use <see cref="Logger"/> directly when you need an isolated instance, for example in tests.
/// </para>
/// </remarks>
public class Logger
{
    #region Sink Management

    private readonly HashSet<ILogSink> Sinks = [];

    /// <inheritdoc cref="HashSet{ILogSink}.Add" />
    public bool AddSink(ILogSink sink)
    {
        return Sinks.Add(sink);
    }

    /// <inheritdoc cref="HashSet{ILogSink}.Remove" />
    public bool RemoveSink(ILogSink sink)
    {
        return Sinks.Remove(sink);
    }

    #endregion

    #region Configure Level

    /// <summary>
    /// The minimum level applied to channels that have no explicit override.
    /// Entries below this level are discarded.
    /// </summary>
    public LogLevel DefaultLevel { get; private set; } = LogLevel.TRACE;

    /// <summary>
    /// The most restrictive level that can be configured.
    /// Attempting to set a level above this cap emits a warning and uses the cap instead,
    /// ensuring <see cref="LogLevel.WARNING"/> and above are never silenced.
    /// </summary>
    public static readonly LogLevel LevelCap = LogLevel.WARNING;

    private readonly Dictionary<Type, LogLevel> ChannelLevels = [];

    /// <summary>
    /// Sets the minimum log level for channels that have no explicit override.
    /// </summary>
    /// <param name="level">The desired level. Capped at <see cref="LevelCap"/> if exceeded.</param>
    public void SetDefaultLevel(LogLevel level) => DefaultLevel = GetCappedLogLevel(level);

    /// <summary>
    /// Sets a minimum log level for a specific channel, overriding <see cref="DefaultLevel"/> for that channel.
    /// </summary>
    /// <typeparam name="TChannel">The channel to configure.</typeparam>
    /// <param name="level">The desired level. Capped at <see cref="LevelCap"/> if exceeded.</param>
    public void SetChannelLevel<TChannel>(LogLevel level) where TChannel : ILogChannel
    {
        ChannelLevels[typeof(TChannel)] = GetCappedLogLevel(level);
    }

    /// <summary>
    /// Removes the level override for a specific channel, causing it to fall back to <see cref="DefaultLevel"/>.
    /// </summary>
    /// <typeparam name="TChannel">The channel whose override should be removed.</typeparam>
    public void ClearChannelLevel<TChannel>() where TChannel : ILogChannel
    {
        ChannelLevels.Remove(typeof(TChannel));
    }

    #endregion

    #region Creating Entries

    private readonly Stopwatch Stopwatch = Stopwatch.StartNew();

    private void Write<TChannel>(LogLevel level, string message)
        where TChannel : ILogChannel
    {
        if (ChannelLevels.TryGetValue(typeof(TChannel), out LogLevel channelLevel))
        {
            if(level < channelLevel) return;
        }
        else if (level < DefaultLevel) return;

        LogEntry<TChannel> entry = level >= LogLevel.ERROR
            ? new LogEntry<TChannel>(Stopwatch.Elapsed, level, message, new StackTrace(skipFrames: 3, fNeedFileInfo: true))
            : new LogEntry<TChannel>(Stopwatch.Elapsed, level, message);

        Sinks.ForEach(sink =>
        {
            sink.Write(entry);
        });
    }

    /// <summary>Logs a message at <see cref="LogLevel.TRACE"/> level on the specified channel.</summary>
    public void Trace<TChannel>(string message) where TChannel : ILogChannel => Write<TChannel>(LogLevel.TRACE, message);
    /// <summary>Logs a message at <see cref="LogLevel.DEBUG"/> level on the specified channel.</summary>
    public void Debug<TChannel>(string message) where TChannel : ILogChannel => Write<TChannel>(LogLevel.DEBUG, message);
    /// <summary>Logs a message at <see cref="LogLevel.INFO"/> level on the specified channel.</summary>
    public void Info<TChannel>(string message) where TChannel : ILogChannel => Write<TChannel>(LogLevel.INFO, message);
    /// <summary>Logs a message at <see cref="LogLevel.WARNING"/> level on the specified channel.</summary>
    public void Warning<TChannel>(string message) where TChannel : ILogChannel => Write<TChannel>(LogLevel.WARNING, message);
    /// <summary>Logs a message at <see cref="LogLevel.ERROR"/> level on the specified channel. Captures a stack trace.</summary>
    public void Error<TChannel>(string message) where TChannel : ILogChannel => Write<TChannel>(LogLevel.ERROR, message);
    /// <summary>Logs a message at <see cref="LogLevel.FATAL"/> level on the specified channel. Captures a stack trace.</summary>
    public void Fatal<TChannel>(string message) where TChannel : ILogChannel => Write<TChannel>(LogLevel.FATAL, message);

    #endregion

    #region Utils

    private LogLevel GetCappedLogLevel(LogLevel level)
    {
        LogLevel cap = LevelCap;
        if (level <= cap) return level;

        string joinedSuppressedWarnings =
            String.Join(
                ", ",
                Enum.GetValues<LogLevel>()
                    .Where(v => v >= cap)
                    .Select(Enum.GetName)
            );
        Warning<GlobalChannel>($"Log level {Enum.GetName(level)} would silence {joinedSuppressedWarnings} which is forbidden. {Enum.GetName(cap)} will be used instead.");
        return cap;
    }

    #endregion

}