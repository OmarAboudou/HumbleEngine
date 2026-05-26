namespace HumbleEngine.Core;

public abstract class SingleChildStatefulWidget<TChild> : StatefulWidget
    where TChild : IWidget
{
    public TChild? Child { get; init; }
}