using System.Text;

namespace HumbleEngine.Core;

public class ConsoleSink : ILogSink
{
    private readonly StringBuilder _stringBuilder = new();
    
    public void Write<TChannel>(LogEntry<TChannel> entry) where TChannel : ILogChannel
    {
        ConsoleColor previousColor = Console.ForegroundColor;
        Console.ForegroundColor = GetColor(entry.Level);

        this._stringBuilder.Clear();
        this._stringBuilder.Append('[');
        this._stringBuilder.Append(entry.TimeSpan.Hours.ToString("D2"));
        this._stringBuilder.Append(':');
        this._stringBuilder.Append(entry.TimeSpan.Minutes.ToString("D2"));
        this._stringBuilder.Append(':');
        this._stringBuilder.Append(entry.TimeSpan.Seconds.ToString("D2"));
        this._stringBuilder.Append('.');
        this._stringBuilder.Append(entry.TimeSpan.Milliseconds.ToString("D3"));
        this._stringBuilder.Append(']');
        this._stringBuilder.Append(' ');
        this._stringBuilder.Append('[');
        this._stringBuilder.Append(entry.Level);
        this._stringBuilder.Append(']');
        this._stringBuilder.Append(' ');
        this._stringBuilder.Append('[');
        this._stringBuilder.Append(TChannel.ChannelName);
        this._stringBuilder.Append(']');
        this._stringBuilder.Append(' ');
        this._stringBuilder.Append(entry.Message);
        this._stringBuilder.Append('\n');
        if(entry.StackTrace != null)
        {
            this._stringBuilder.Append(entry.StackTrace);
        }
        Console.Write(this._stringBuilder.ToString());
        
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