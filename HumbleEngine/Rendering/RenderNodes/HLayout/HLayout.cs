using System.Collections;
using System.Runtime.CompilerServices;

namespace HumbleEngine;

[CollectionBuilder(typeof(HLayout), nameof(Create))]
public struct HLayout : ICompositeRenderElement
{
    private LinearLayoutData _data;
    private LayoutData       _layout;
    private List<RenderDescription>? _children;

    public LayoutData Layout { get => _layout; set => _layout = value; }

    public HLayout Spacing(float spacing) { _data = _data with { Spacing = spacing }; return this; }

    public void Add(RenderDescription child)
    {
        _children ??= new List<RenderDescription>();
        _children.Add(child);
    }

    public static HLayout Create(ReadOnlySpan<RenderDescription> items)
        => RenderElementExtensions.Create<HLayout>(items);

    public static implicit operator RenderDescription(HLayout h) => new()
    {
        Kind         = RenderEntryKind.HLayout,
        LinearLayout = h._data,
        Layout       = h._layout,
        Children     = h._children?.ToArray() ?? Array.Empty<RenderDescription>()
    };

    public IEnumerator<RenderDescription> GetEnumerator() =>
        (_children ?? Enumerable.Empty<RenderDescription>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
