using System.Collections;

namespace HumbleEngine;

public struct VLayout : ICompositeRenderNode
{
    private VLayoutData _data;
    private LayoutData  _layout;
    private List<RenderDescription>? _children;

    public LayoutData Layout { get => _layout; set => _layout = value; }

    public VLayout Spacing(float spacing) { _data = _data with { Spacing = spacing }; return this; }

    public void Add(RenderDescription child)
    {
        _children ??= new List<RenderDescription>();
        _children.Add(child);
    }

    public static implicit operator RenderDescription(VLayout v) => new()
    {
        Kind     = RenderNodeKind.VLayout,
        VLayout  = v._data,
        Layout   = v._layout,
        Children = v._children?.ToArray() ?? Array.Empty<RenderDescription>()
    };

    public IEnumerator<RenderDescription> GetEnumerator() =>
        (_children ?? Enumerable.Empty<RenderDescription>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
