using System.Collections;

namespace HumbleEngine;

public struct Column : IEnumerable<RenderDescription>
{
    private List<RenderDescription>? _children;

    public void Add(RenderDescription child)
    {
        _children ??= new List<RenderDescription>();
        _children.Add(child);
    }

    public static implicit operator RenderDescription(Column col) => new()
    {
        Kind     = RenderNodeKind.Column,
        Children = col._children?.ToArray() ?? Array.Empty<RenderDescription>()
    };

    public IEnumerator<RenderDescription> GetEnumerator() =>
        (_children ?? Enumerable.Empty<RenderDescription>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
