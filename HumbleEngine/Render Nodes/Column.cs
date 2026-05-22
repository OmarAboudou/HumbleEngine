namespace HumbleEngine;

// Builder pour un conteneur vertical.
// Supporte la syntaxe collection initializer : new Column { child1, child2 }
// Les éléments sont automatiquement convertis en RenderDescription via implicit operators.
public struct Column
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
}
