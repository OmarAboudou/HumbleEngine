using System.Text;

namespace HumbleEngine.Core;

public class ConsoleSink : ILogSink
{
    private readonly StringBuilder _stringBuilder = new();
    
    public void Write<TChannel>(LogEntry<TChannel> entry) where TChannel : ILogChannel
    {
        ConsoleColor previousColor = Console.ForegroundColor;
        Console.ForegroundColor = GetColor(entry.Level);

        _stringBuilder.Clear();
        _stringBuilder.Append('[');
        _stringBuilder.Append(entry.TimeSpan.Hours.ToString("D2"));
        _stringBuilder.Append(':');
        _stringBuilder.Append(entry.TimeSpan.Minutes.ToString("D2"));
        _stringBuilder.Append(':');
        _stringBuilder.Append(entry.TimeSpan.Seconds.ToString("D2"));
        _stringBuilder.Append('.');
        _stringBuilder.Append(entry.TimeSpan.Milliseconds.ToString("D3"));
        _stringBuilder.Append(']');
        _stringBuilder.Append(' ');
        _stringBuilder.Append('[');
        _stringBuilder.Append(entry.Level);
        _stringBuilder.Append(']');
        _stringBuilder.Append(' ');
        _stringBuilder.Append('[');
        _stringBuilder.Append(TChannel.ChannelName);
        _stringBuilder.Append(']');
        _stringBuilder.Append(' ');
        _stringBuilder.Append(entry.Message);
        _stringBuilder.Append('\n');
        if(entry.StackTrace != null)
        {
            _stringBuilder.Append(entry.StackTrace);
        }
        Console.Write(_stringBuilder.ToString());
        
        Console.ForegroundColor = previousColor;
    }

    private static ConsoleColor GetColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.TRACE => ConsoleColor.DarkGray,
            LogLevel.DEBUG => ConsoleColor.Gray,
            LogLevel.INFO  => ConsoleColor.White,
            LogLevel.WARNING  => ConsoleColor.Yellow,
            LogLevel.ERROR => ConsoleColor.Red,
            LogLevel.FATAL => ConsoleColor.DarkRed,
            _              => ConsoleColor.White
        };
    }
    
}