using System.Collections;
using System.Runtime.CompilerServices;

namespace HumbleEngine;

[CollectionBuilder(typeof(VLayout), nameof(Create))]
public struct VLayout : ICompositeRenderElement
{
    private LinearLayoutData _data;
    private LayoutData       _layout;
    private List<RenderDescription>? _children;

    public LayoutData Layout { get => _layout; set => _layout = value; }

    public VLayout Spacing(float spacing) { _data = _data with { Spacing = spacing }; return this; }

    public void Add(RenderDescription child)
    {
        _children ??= new List<RenderDescription>();
        _children.Add(child);
    }

    public static VLayout Create(ReadOnlySpan<RenderDescription> items)
        => RenderElementExtensions.Create<VLayout>(items);

    public static implicit operator RenderDescription(VLayout v) => new()
    {
        Kind         = RenderEntryKind.VLayout,
        LinearLayout = v._data,
        Layout       = v._layout,
        Children     = v._children?.ToArray() ?? Array.Empty<RenderDescription>()
    };

    public IEnumerator<RenderDescription> GetEnumerator() =>
        (_children ?? Enumerable.Empty<RenderDescription>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
