using System.Collections;
using SkiaSharp;

namespace HumbleEngine;

public struct Box : IEnumerable<RenderDescription>
{
    private readonly BoxData    _boxData;
    private readonly LayoutData _layout;
    private List<RenderDescription>? _children;

    public Box(SKColor color, float cornerRadius = 0f,
               float? width = null, float? height = null,
               float paddingX = 0f, float paddingY = 0f)
    {
        _boxData = new BoxData { BackgroundColor = color, CornerRadius = cornerRadius };
        _layout  = new LayoutData { Width = width, Height = height, PaddingX = paddingX, PaddingY = paddingY };
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
