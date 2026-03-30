using System.Diagnostics;

namespace HumbleEngine.Core;

/// <summary>
/// A log entry with untyped channel information, used for storage and generic processing.
/// </summary>
/// <param name="TimeSpan">Time elapsed since the logger was created.</param>
/// <param name="Level">Severity of the entry.</param>
/// <param name="Channel">The <see cref="Type"/> of the channel this entry was logged on.</param>
/// <param name="Message">The log message.</param>
/// <param name="StackTrace">Stack trace captured at the log call site, only present for <see cref="LogLevel.ERROR"/> and above.</param>
public record LogEntry(
    TimeSpan TimeSpan,
    LogLevel Level,
    Type Channel,
    string Message,
    StackTrace? StackTrace = null
);

/// <summary>
/// A log entry carrying compile-time channel type information.
/// </summary>
/// <typeparam name="TChannel">The channel this entry was logged on.</typeparam>
/// <param name="TimeSpan">Time elapsed since the logger was created.</param>
/// <param name="Level">Severity of the entry.</param>
/// <param name="Message">The log message.</param>
/// <param name="StackTrace">Stack trace captured at the log call site, only present for <see cref="LogLevel.ERROR"/> and above.</param>
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