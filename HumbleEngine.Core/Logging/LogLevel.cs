namespace HumbleEngine.Core;

/// <summary>
/// Defines the severity levels for log entries, from least to most critical.
/// </summary>
/// <remarks>
/// Levels are ordered numerically: a logger configured at a given level will
/// only emit entries at that level or above.
/// </remarks>
public enum LogLevel
{
    /// <summary>Fine-grained diagnostic information, typically only useful during development.</summary>
    TRACE,
    /// <summary>General debugging information.</summary>
    DEBUG,
    /// <summary>Informational messages about normal operation.</summary>
    INFO,
    /// <summary>Unexpected situations that are recoverable but worth attention.</summary>
    WARNING,
    /// <summary>Errors that affect the current operation but not the whole application.</summary>
    ERROR,
    /// <summary>Critical failures that may compromise the stability of the application.</summary>
    FATAL
}