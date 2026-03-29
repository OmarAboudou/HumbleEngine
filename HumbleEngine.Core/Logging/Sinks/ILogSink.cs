namespace HumbleEngine.Core;

public interface ILogSink
{
    public void Write<TChannel>(LogEntry<TChannel> logEntry) where TChannel : ILogChannel;
}