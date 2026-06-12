namespace HumbleEngine;

/// <summary>
/// Shared machinery of <see cref="Column"/> and <see cref="Row"/>: stacks the
/// UI children along one axis, separated by <see cref="Spacing"/>. A container
/// opens its composition (<see cref="Children"/>) — the engine rule: containers
/// open, scenes close.
/// <para>
/// The layout is <b>reactive, not per frame</b>: children positions are
/// recomputed when — and only when — an observable changes: the children list
/// (add/remove, including departures behind the list's back), a child's
/// <see cref="UINode.Size"/>, or <see cref="Spacing"/>. The layout writes
/// <see cref="UINode.Position"/> and depends only on sizes and order, so no
/// feedback loop is possible.
/// </para>
/// </summary>
public abstract class LinearContainer : UINode
{
    /// <summary>UI children, in stacking order. Adding adopts, removing detaches.</summary>
    public new NodeList<UINode> Children { get; }

    /// <summary>Gap in pixels between two consecutive children.</summary>
    public Reactive<float> Spacing { get; }

    private protected LinearContainer()
    {
        Children = CreateChildList<UINode>();
        Spacing  = CreateReactive(0f);
        Children.Added   += OnChildJoined;
        Children.Removed += OnChildLeft;
        Spacing.Changed  += _ => Relayout();
    }

    /// <summary>Relative position of a child whose main-axis offset is <paramref name="mainOffset"/>.</summary>
    private protected abstract Vector2 Place(float mainOffset);

    /// <summary>The size's extent along the stacking axis.</summary>
    private protected abstract float MainExtent(Vector2 size);

    /// <summary>
    /// Restacks every child: walk in order, place each at the running offset,
    /// advance by its main-axis extent plus the spacing.
    /// </summary>
    private void Relayout()
    {
        var offset = 0f;
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            child.Position.Value = Place(offset);
            offset += MainExtent(child.Size.Value) + Spacing.Value;
        }
    }

    private void OnChildJoined(int index, UINode child)
    {
        child.Size.Changed += OnChildSizeChanged;
        Relayout();
    }

    private void OnChildLeft(int index, UINode child)
    {
        child.Size.Changed -= OnChildSizeChanged;
        Relayout();
    }

    private void OnChildSizeChanged(Vector2 newSize) => Relayout();
}

/// <summary>
/// Stacks its children vertically, top to bottom — the child's
/// <see cref="UINode.Size"/> height drives the stacking, its width is left
/// untouched (no stretch on this étage).
/// </summary>
public sealed class Column : LinearContainer
{
    private protected override Vector2 Place(float mainOffset) => new(0f, mainOffset);

    private protected override float MainExtent(Vector2 size) => size.Y;
}

/// <summary>
/// Stacks its children horizontally, left to right — the child's
/// <see cref="UINode.Size"/> width drives the stacking, its height is left
/// untouched (no stretch on this étage).
/// </summary>
public sealed class Row : LinearContainer
{
    private protected override Vector2 Place(float mainOffset) => new(mainOffset, 0f);

    private protected override float MainExtent(Vector2 size) => size.X;
}
