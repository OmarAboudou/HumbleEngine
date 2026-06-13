namespace HumbleEngine;

/// <summary>
/// Shared machinery of <see cref="Column"/> and <see cref="Row"/>: stacks the
/// UI children along one axis, separated by <see cref="Spacing"/>. A container
/// opens its composition (<see cref="Children"/>) — the engine rule: containers
/// open, scenes close.
/// <para>
/// The layout is <b>reactive, not per frame</b>: it is a single auto-tracking
/// <see cref="Effect"/>. Restacking <i>reads</i> the children structure
/// (<see cref="NodeList{TChild}.Count"/>, the indexer), each child's
/// <see cref="UINode.Size"/>, and <see cref="Spacing"/> — so the effect
/// re-subscribes to exactly those each run and re-runs when any changes, including
/// children joining or leaving (the structure signal) and departures behind the
/// list's back. No manual <c>.Changed</c>/<c>Added</c> bookkeeping, and no
/// per-child subscription to maintain: the reads list the dependencies. The effect
/// writes <see cref="UINode.Position"/>, which it never reads, so no feedback loop
/// is possible.
/// </para>
/// </summary>
public abstract class LinearContainer : UINode
{
    /// <summary>UI children, in stacking order. Adding adopts, removing detaches.</summary>
    public new NodeList<UINode> Children { get; }

    /// <summary>Gap in pixels between two consecutive children.</summary>
    public Property<float> Spacing { get; }

    private protected LinearContainer()
    {
        Children = CreateChildList<UINode>();
        Spacing  = CreateProperty(0f);
        CreateEffect(Relayout);
    }

    /// <summary>Relative position of a child whose main-axis offset is <paramref name="mainOffset"/>.</summary>
    private protected abstract Vector2 Place(float mainOffset);

    /// <summary>The size's extent along the stacking axis.</summary>
    private protected abstract float MainExtent(Vector2 size);

    /// <summary>
    /// Restacks every child: walk in order, place each at the running offset,
    /// advance by its main-axis extent plus the spacing. Run inside an
    /// <see cref="Effect"/> — every reactive read here becomes a dependency.
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
