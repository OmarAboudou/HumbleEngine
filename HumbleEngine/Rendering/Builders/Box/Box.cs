using SkiaSharp;

namespace HumbleEngine;

public struct Box
{
    private readonly SKColor _color;
    private readonly float   _cornerRadius;
    private List<RenderDescription>? _children;

    public Box(SKColor color, float cornerRadius = 0f)
    {
        _color        = color;
        _cornerRadius = cornerRadius;
    }

    public void Add(RenderDescription child)
    {
        _children ??= new List<RenderDescription>();
        _children.Add(child);
    }

    public static implicit operator RenderDescription(Box b) => new()
    {
        Kind     = RenderNodeKind.Box,
        Box      = new BoxData { BackgroundColor = b._color, CornerRadius = b._cornerRadius },
        Children = b._children?.ToArray() ?? Array.Empty<RenderDescription>()
    };
}
