namespace HumbleEngine.Core;

public interface ILogSink
{
    public void Write(LogEntry logEntry);
}