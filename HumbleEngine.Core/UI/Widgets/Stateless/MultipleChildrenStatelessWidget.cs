namespace HumbleEngine.Core;

public abstract record MultipleChildrenStatelessWidget<TChildren> : StatelessWidget
    where TChildren : IWidget
{
    public IReadOnlyList<TChildren> Children { get; init; } = [];
}