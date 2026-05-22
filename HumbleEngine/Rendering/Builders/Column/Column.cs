using System.Collections;

namespace HumbleEngine;

public struct Column : ICompositeRenderNode
{
    private readonly ColumnData _columnData;
    private LayoutData          _layout;
    private List<RenderDescription>? _children;

    public LayoutData Layout { get => _layout; set => _layout = value; }

    public Column(float spacing = 0f)
    {
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
