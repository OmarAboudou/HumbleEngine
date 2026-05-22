using System.Collections;

namespace HumbleEngine;

public struct Column : IEnumerable<RenderDescription>
{
    private readonly LayoutData  _layout;
    private readonly ColumnData  _columnData;
    private List<RenderDescription>? _children;

    public Column(float spacing = 0f,
                  float? width = null, float? height = null,
                  float paddingX = 0f, float paddingY = 0f)
    {
        _layout     = new LayoutData { Width = width, Height = height, PaddingX = paddingX, PaddingY = paddingY };
        _columnData = new ColumnData { Spacing = spacing };
    }

    public void Add(RenderDescription child)
    {
        _children ??= new List<RenderDescription>();
        _children.Add(child);
    }

    public static implicit operator RenderDescription(Column col) => new()
    {
        Kind     = RenderNodeKind.Column,
        Layout   = col._layout,
        Column   = col._columnData,
        Children = col._children?.ToArray() ?? Array.Empty<RenderDescription>()
    };

    public IEnumerator<RenderDescription> GetEnumerator() =>
        (_children ?? Enumerable.Empty<RenderDescription>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
