using System.Diagnostics;

namespace HumbleEngine.Core;

public static class Logger
{
    private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

    private static readonly HashSet<ILogSink> Sinks = [];
    
    public static LogLevel MinimumLogLevel { get; private set; }
    
    private static readonly Dictionary<Type, LogLevel> ChannelLevels = [];

    static Logger()
    {
        AddSink(new ConsoleSink());
        SetMinimumLogLevel(LogLevel.TRACE);
    }

    public static void SetMinimumLogLevel(LogLevel level) 
        => MinimumLogLevel = level > LogLevel.ERROR ? LogLevel.ERROR : level;

    #region Sink Management

    /// <inheritdoc cref="HashSet{ILogSink}.Add" />
    public static bool AddSink(ILogSink sink)
    {
        return Sinks.Add(sink);
    }

    /// <inheritdoc cref="HashSet{ILogSink}.Remove" />
    public static bool RemoveSink(ILogSink sink)
    {
        return Sinks.Remove(sink);
    }

    #endregion
    
    #region Configure Channel Level
    
    public static void SetChannelLevel<TChannel>(LogLevel level) where TChannel : ILogChannel
    {
        ChannelLevels[typeof(TChannel)] = level > LogLevel.ERROR ? LogLevel.ERROR : level;
    }
    public static void ClearChannelLevel<TChannel>() where TChannel : ILogChannel
    {
        ChannelLevels.Remove(typeof(TChannel));
    }    
    
    #endregion

    #region Creating Entries

    private static void Write<TChannel>(LogLevel level, string message) where TChannel : ILogChannel
    {
        if (ChannelLevels.TryGetValue(typeof(TChannel), out LogLevel channelLevel))
        {
            if(level < channelLevel) return;
        }
        else if (level < MinimumLogLevel) return;

        LogEntry<TChannel> entry = level >= LogLevel.ERROR
            ? new LogEntry<TChannel>(Stopwatch.Elapsed, level, message, new StackTrace(skipFrames: 3, fNeedFileInfo: true))
            : new LogEntry<TChannel>(Stopwatch.Elapsed, level, message);
        
        Sinks.ForEach(sink =>
        {
            sink.Write(entry);
        });
    }
    
    public static void Trace<TChannel>(string message) where TChannel : ILogChannel => Write<TChannel>(LogLevel.TRACE, message);
    public static void Debug<TChannel>(string message)  where TChannel : ILogChannel => Write<TChannel>(LogLevel.DEBUG, message);
    public static void Info<TChannel>(string message)  where TChannel : ILogChannel => Write<TChannel>(LogLevel.INFO, message);
    public static void Warning<TChannel>(string message)  where TChannel : ILogChannel => Write<TChannel>(LogLevel.WARNING, message);
    public static void Error<TChannel>(string message)  where TChannel : ILogChannel => Write<TChannel>(LogLevel.ERROR, message);
    public static void Fatal<TChannel>(string message)  where TChannel : ILogChannel => Write<TChannel>(LogLevel.FATAL, message);

    #endregion
    
}