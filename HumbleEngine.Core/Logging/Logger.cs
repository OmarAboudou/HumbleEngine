using System.Diagnostics;

namespace HumbleEngine.Core;

public static class Logger
{
    private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();
    
    public static readonly HashSet<ILogSink> Sinks = new();
    
    public static LogLevel DefaultLogLevel { get; set; } = LogLevel.TRACE;
    
    private static readonly Dictionary<Type, LogLevel> ChannelLevels = new();

    public static void SetChannelLevel<TChannel>(LogLevel level) where TChannel : ILogChannel
    {
        ChannelLevels[typeof(TChannel)] = level;    
    }
    public static void ClearChannelLevel<TChannel>() where TChannel : ILogChannel
    {
        ChannelLevels.Remove(typeof(TChannel));
    }

    
    public static void Info(LogEntry entry)
    {
        
    }
    
}