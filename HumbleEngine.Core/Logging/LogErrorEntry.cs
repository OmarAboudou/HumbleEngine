using System.Diagnostics;

namespace HumbleEngine.Core;

public record LogErrorEntry(
    TimeSpan TimeSpan,
    LogLevel Level,
    ILogChannel Channel,
    string Message,
    StackTrace StackTrace
) : LogEntry(
    TimeSpan,
    Level,
    Channel,
    Message
);