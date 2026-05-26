namespace HumbleEngine.Core;

public abstract class MultipleChildrenStatefulWidget<TChildren> : StatefulWidget
    where TChildren : IWidget
{
    public IReadOnlyList<TChildren> Children { get; init; } = [];
}