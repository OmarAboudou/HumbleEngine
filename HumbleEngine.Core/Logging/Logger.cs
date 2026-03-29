using System.Diagnostics;

namespace HumbleEngine.Core;

public static class Logger
{
    private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

    public static readonly HashSet<ILogSink> Sinks = [];
    
    public static LogLevel DefaultLogLevel { get; set; } = LogLevel.TRACE;
    
    private static readonly Dictionary<Type, LogLevel> ChannelLevels = new();

    static Logger()
    {
        Sinks.Add(new ConsoleSink());
    }
    
    public static void SetChannelLevel<TChannel>(LogLevel level) where TChannel : ILogChannel
    {
        ChannelLevels[typeof(TChannel)] = level;
    }
    public static void ClearChannelLevel<TChannel>() where TChannel : ILogChannel
    {
        ChannelLevels.Remove(typeof(TChannel));
    }

    private static void Write<TChannel>(LogLevel level, string message) where TChannel : ILogChannel
    {
        if (ChannelLevels.TryGetValue(typeof(TChannel), out LogLevel channelLevel))
        {
            if(level < channelLevel) return;
        }
        if (level < DefaultLogLevel) return;

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
    
}