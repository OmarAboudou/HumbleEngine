using System.Collections;
using SkiaSharp;

namespace HumbleEngine;

public struct Box : ICompositeRenderNode
{
    private readonly BoxData _boxData;
    private LayoutData       _layout;
    private List<RenderDescription>? _children;

    public LayoutData Layout { get => _layout; set => _layout = value; }

    public Box(SKColor color, float cornerRadius = 0f)
    {
        _boxData = new BoxData { BackgroundColor = color, CornerRadius = cornerRadius };
    }

    public void Add(RenderDescription child)
    {
        _children ??= new List<RenderDescription>();
        _children.Add(child);
    }

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
