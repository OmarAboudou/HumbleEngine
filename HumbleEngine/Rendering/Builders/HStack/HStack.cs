using System.Collections;

namespace HumbleEngine;

public struct HStack : ICompositeRenderNode
{
    private RowData    _rowData;
    private LayoutData _layout;
    private List<RenderDescription>? _children;

    public LayoutData Layout { get => _layout; set => _layout = value; }

    public HStack Spacing(float spacing) { _rowData = _rowData with { Spacing = spacing }; return this; }

    public void Add(RenderDescription child)
    {
        _children ??= new List<RenderDescription>();
        _children.Add(child);
    }

    public static implicit operator RenderDescription(HStack h) => new()
    {
        Kind     = RenderNodeKind.Row,
        Layout   = h._layout,
        Row      = h._rowData,
        Children = h._children?.ToArray() ?? Array.Empty<RenderDescription>()
    };

    public IEnumerator<RenderDescription> GetEnumerator() =>
        (_children ?? Enumerable.Empty<RenderDescription>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
