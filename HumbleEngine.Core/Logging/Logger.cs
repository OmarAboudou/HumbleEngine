using System.Diagnostics;

namespace HumbleEngine.Core;

public class Logger
{
    private readonly Stopwatch Stopwatch = Stopwatch.StartNew();

    private readonly HashSet<ILogSink> Sinks = [];
    
    public LogLevel MinimumLogLevel { get; private set; }
    
    private readonly Dictionary<Type, LogLevel> ChannelLevels = [];

    public Logger()
    {
        this.AddSink(new ConsoleSink());
        this.SetMinimumLogLevel(LogLevel.TRACE);
    }

    public void SetMinimumLogLevel(LogLevel level) => MinimumLogLevel = GetCappedLogLevel(level);
    
    #region Sink Management

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
    
    #region Configure Channel Level
    
    public void SetChannelLevel<TChannel>(LogLevel level) where TChannel : ILogChannel
    {
        ChannelLevels[typeof(TChannel)] = GetCappedLogLevel(level);
    }
    public void ClearChannelLevel<TChannel>() where TChannel : ILogChannel
    {
        ChannelLevels.Remove(typeof(TChannel));
    }    
    
    #endregion

    #region Creating Entries

    private void Write<TChannel>(LogLevel level, string message) where TChannel : ILogChannel
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
    
    public void Trace<TChannel>(string message) where TChannel : ILogChannel => Write<TChannel>(LogLevel.TRACE, message);
    public void Debug<TChannel>(string message)  where TChannel : ILogChannel => Write<TChannel>(LogLevel.DEBUG, message);
    public void Info<TChannel>(string message)  where TChannel : ILogChannel => Write<TChannel>(LogLevel.INFO, message);
    public void Warning<TChannel>(string message)  where TChannel : ILogChannel => Write<TChannel>(LogLevel.WARNING, message);
    public void Error<TChannel>(string message)  where TChannel : ILogChannel => Write<TChannel>(LogLevel.ERROR, message);
    public void Fatal<TChannel>(string message)  where TChannel : ILogChannel => Write<TChannel>(LogLevel.FATAL, message);

    #endregion

    #region Utils

    private LogLevel GetCappedLogLevel(LogLevel level)
    {
        LogLevel cap = LogLevel.WARNING;
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