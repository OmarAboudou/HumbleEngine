using System.Diagnostics;

namespace HumbleEngine.Core;

public record LogEntry(
    TimeSpan TimeSpan,
    LogLevel Level,
    ILogChannel Channel,
    string Message,
    StackTrace? StackTrace
);