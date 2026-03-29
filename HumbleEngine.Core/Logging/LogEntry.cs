using System.Diagnostics;

namespace HumbleEngine.Core;

public record LogEntry(
    TimeSpan TimeSpan,
    LogLevel Level,
    Type Channel,
    string Message,
    StackTrace? StackTrace = null
);

public record LogEntry<TChannel>(
    TimeSpan TimeSpan,
    LogLevel Level,
    string Message,
    StackTrace? StackTrace = null
) : LogEntry(
    TimeSpan,
    Level,
    typeof(TChannel),
    Message,
    StackTrace
);