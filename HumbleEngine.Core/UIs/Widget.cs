namespace HumbleEngine.Core.UIs;

public abstract record Widget : HumbleRecord
{
    public object? Key { get; init; }

}