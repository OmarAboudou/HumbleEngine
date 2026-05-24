namespace HumbleEngine;

public sealed class UILayoutCache
{
    public IReadOnlyList<(UINode Node, LayoutNode Layout)> Roots { get; }

    public UILayoutCache(List<(UINode Node, LayoutNode Layout)> roots)
        => Roots = roots;
}
