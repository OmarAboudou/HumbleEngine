namespace HumbleEngine.Core;

public abstract record Widget
{
    public object? Key { get; init; }
    public object? GlobalKey { get; init; }
}