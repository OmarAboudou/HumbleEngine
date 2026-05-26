namespace HumbleEngine.Core;

public interface IMultipleChildrenWidget<TChildren> : IWidget, IEnumerable<TChildren>
    where TChildren : IWidget
{
    public IReadOnlyList<TChildren> Children { get; init; }
    
    public void Add(TChildren child);

    public void Add(IEnumerable<TChildren> children)
    {
        foreach (TChildren widget in children) 
            Add(widget);
    }
}

public static class IMultipleChildrenWidgetExtensions
{
    public static void Add<TChildren>(this IMultipleChildrenWidget<TChildren> self, IEnumerable<TChildren> children)
        where TChildren : struct, IWidget =>
        self.Add(children);
}