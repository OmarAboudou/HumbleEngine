namespace HumbleEngine.Core;

public abstract record SingleChildStatelessWidget<TChild> : StatelessWidget
    where TChild : IWidget
{
    public TChild? Child { get; init; }
}