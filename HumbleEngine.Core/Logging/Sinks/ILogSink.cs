namespace HumbleEngine.Core;

/// <summary>
/// Represents a destination for log entries, such as the console, a file, or an in-memory buffer.
/// </summary>
/// <remarks>
/// Implement this interface to create a custom sink.
/// A <see cref="Logger"/> can have multiple sinks active simultaneously.
/// </remarks>
public interface ILogSink
{
    /// <summary>
    /// Writes a log entry to this sink.
    /// </summary>
    /// <typeparam name="TChannel">The channel the entry was logged on.</typeparam>
    /// <param name="logEntry">The entry to write.</param>
    public void Write<TChannel>(LogEntry<TChannel> logEntry) where TChannel : ILogChannel;
}