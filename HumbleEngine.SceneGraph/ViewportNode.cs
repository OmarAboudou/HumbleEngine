namespace HumbleEngine;

/// <summary>
/// A <see cref="UINode"/> that hosts and displays a scene sub-tree inside a
/// bounded, coloured rect — the "viewport" building block of the editor and any
/// split-view UI. One content root is accepted at a time through a
/// <see cref="NodeSlot{TChild}"/>; the slot adopts it, and the standard tree
/// traversal renders it: sub-tree nodes draw with their
/// <see cref="UINode.GlobalRect"/> naturally offset by this viewport's own
/// position, with no extra renderer API needed.
/// <para>
/// No scissor on this étage: nodes may overflow the viewport boundary,
/// consistent with how the other containers work. Clipping will be added as a
/// renderer capability when a concrete use-case demands it.
/// </para>
/// </summary>
public sealed class ViewportNode : UINode
{
    private static readonly Vector4 DefaultBackground = new(0.08f, 0.08f, 0.10f, 1f);

    private readonly NodeSlot<Node> _content;

    /// <summary>Background colour painted behind the content, every frame.</summary>
    public Property<Vector4> Background { get; }

    public ViewportNode()
    {
        Background = CreateProperty(DefaultBackground);
        _content   = CreateChildSlot<Node>();
    }

    /// <summary>
    /// The scene root displayed inside this viewport. Setting adopts the node
    /// (from wherever it sits) and attaches it as a child; the previous root is
    /// detached and returned to the caller. Null = empty viewport.
    /// </summary>
    public Node? Content
    {
        get => _content.Value;
        set => _content.Value = value;
    }

    /// <inheritdoc />
    protected override void OnDraw(IRenderer renderer)
    {
        renderer.DrawQuad(GlobalRect, Background.Value);
    }
}
