namespace HumbleEngine.Core;

public static class Log
{
    private static Logger _instance = new Logger();

    #region Sink Management
    
    public static bool AddSink(ILogSink sink) 
        => _instance.AddSink(sink); 
    public static bool RemoveSink(ILogSink sink) 
        => _instance.RemoveSink(sink);

    #endregion

    #region Configure Level

    public static void SetDefaultLevel(LogLevel level) => _instance.SetDefaultLevel(level);
    
    public static void SetChannelLevel<TChannel>(LogLevel level) where TChannel : ILogChannel
        =>  _instance.SetChannelLevel<TChannel>(level);
    public static void ClearChannelLevel<TChannel>() where TChannel : ILogChannel
        =>  _instance.ClearChannelLevel<TChannel>();

    #endregion

    #region Creating Entries

    public static void Trace<TChannel>(string message) where TChannel : ILogChannel 
        => _instance.Trace<TChannel>(message);
    public static void Debug<TChannel>(string message)  where TChannel : ILogChannel 
        => _instance.Debug<TChannel>(message);
    public static void Info<TChannel>(string message)  where TChannel : ILogChannel 
        => _instance.Info<TChannel>(message);
    public static void Warning<TChannel>(string message)  where TChannel : ILogChannel 
        => _instance.Warning<TChannel>(message);
    public static void Error<TChannel>(string message)  where TChannel : ILogChannel 
        => _instance.Error<TChannel>(message);
    public static void Fatal<TChannel>(string message)  where TChannel : ILogChannel 
        => _instance.Fatal<TChannel>(message);

    #endregion
    
    
}