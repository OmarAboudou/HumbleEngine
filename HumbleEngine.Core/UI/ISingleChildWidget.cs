namespace HumbleEngine.Core;

public interface ISingleChildWidget<TChild> : IWidget
    where TChild : IWidget
{
    public TChild Child { get; init; }
}