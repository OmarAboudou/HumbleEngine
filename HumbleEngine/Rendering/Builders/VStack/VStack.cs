using System.Collections;

namespace HumbleEngine;

public struct VStack : ICompositeRenderNode
{
    private ColumnData _columnData;
    private LayoutData _layout;
    private List<RenderDescription>? _children;

    public LayoutData Layout { get => _layout; set => _layout = value; }

    public VStack Spacing(float spacing) { _columnData = _columnData with { Spacing = spacing }; return this; }

    public void Add(RenderDescription child)
    {
        _children ??= new List<RenderDescription>();
        _children.Add(child);
    }

    public static implicit operator RenderDescription(VStack v) => new()
    {
        Kind     = RenderNodeKind.Column,
        Layout   = v._layout,
        Column   = v._columnData,
        Children = v._children?.ToArray() ?? Array.Empty<RenderDescription>()
    };

    public IEnumerator<RenderDescription> GetEnumerator() =>
        (_children ?? Enumerable.Empty<RenderDescription>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
