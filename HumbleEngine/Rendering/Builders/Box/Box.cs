using System.Collections;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace HumbleEngine;

[CollectionBuilder(typeof(Box), nameof(Create))]
public struct Box : ICompositeRenderNode
{
    private BoxData   _boxData;
    private LayoutData _layout;
    private List<RenderDescription>? _children;

    public LayoutData Layout { get => _layout; set => _layout = value; }

    public Box Color(SKColor color)       { _boxData = _boxData with { BackgroundColor = color }; return this; }
    public Box CornerRadius(float radius) { _boxData = _boxData with { CornerRadius    = radius }; return this; }

    public void Add(RenderDescription child)
    {
        _children ??= new List<RenderDescription>();
        _children.Add(child);
    }

    public static Box Create(ReadOnlySpan<RenderDescription> items)
        => LayoutExtensions.Create<Box>(items);

    public static implicit operator RenderDescription(Box b) => new()
    {
        Kind     = RenderNodeKind.Box,
        Box      = b._boxData,
        Layout   = b._layout,
        Children = b._children?.ToArray() ?? Array.Empty<RenderDescription>()
    };

    public IEnumerator<RenderDescription> GetEnumerator() =>
        (_children ?? Enumerable.Empty<RenderDescription>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
