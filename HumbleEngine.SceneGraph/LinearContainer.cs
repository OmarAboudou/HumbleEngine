namespace HumbleEngine;

/// <summary>
/// Shared machinery of <see cref="Column"/> and <see cref="Row"/>: stacks the
/// UI children along one axis, separated by <see cref="Spacing"/>, and measures
/// itself from them (content sizing). A container opens its composition
/// (<see cref="Children"/>) — the engine rule: containers open, scenes close.
/// <para>
/// The container is a node in the layout protocol ("constraints down, sizes up"):
/// its <see cref="ComputeLayout"/> poses each child's <see cref="UINode.Incoming"/>
/// (a loosened copy of its own constraint — children take their content size),
/// reads the child's resulting <see cref="UINode.Size"/>, places it at the running
/// main-axis offset, and returns its own size (main = the children's extents plus
/// the spacings, cross = the widest child). It reads the children structure
/// (<see cref="NodeList{TChild}.Count"/>, the indexer), each child's size, and
/// <see cref="Spacing"/> — so the per-node layout effect re-subscribes to exactly
/// those and re-runs when any changes, including children joining or leaving.
/// No stretch on this étage (that is the flex bloc); cross-axis children keep their
/// own extent.
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
    }

    /// <summary>Each child carries a <see cref="FlexParentData"/> — its share of the main axis.</summary>
    protected override ParentData? CreateParentData() => new FlexParentData();

    /// <summary>Adds a non-flex child (content-sized) — sugar for <c>Children.Add</c>.</summary>
    public void Add(UINode child) => Children.Add(child);

    /// <summary>
    /// Adds a child that fills its share of the free main-axis space
    /// (<see cref="Expanded"/>): the descriptor dissolves into the child's
    /// <see cref="FlexParentData"/>, no extra node enters the tree.
    /// </summary>
    public void Add(Expanded child)
    {
        Children.Add(child.Child);
        var flex = (FlexParentData)child.Child.ParentData!;
        flex.Factor.Value = child.Factor;
        flex.Tight.Value  = true;
    }

    /// <summary>Adds a child that may be smaller than its share (<see cref="Flexible"/>, loose fit).</summary>
    public void Add(Flexible child)
    {
        Children.Add(child.Child);
        var flex = (FlexParentData)child.Child.ParentData!;
        flex.Factor.Value = child.Factor;
        flex.Tight.Value  = false;
    }

    /// <summary>
    /// Stacks the children and measures the container. Poses each child's
    /// <see cref="UINode.Incoming"/> (loosened — content sizing), reads its
    /// <see cref="UINode.Size"/>, places it, and accumulates the main extent (plus
    /// spacing between children) and the maximum cross extent. Returns the content
    /// size clamped to <paramref name="constraints"/>.
    /// </summary>
    protected override Vector2 ComputeLayout(Constraints constraints)
    {
        var childConstraints = constraints.Loosen();
        var main  = 0f;
        var cross = 0f;
        var count = Children.Count;
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
                main += Spacing.Value;
            var child = Children[i];
            child.Incoming.Value = childConstraints;   // ↓ constrain
            var size = child.Size.Value;               // ↑ read the result (eager)
            child.Position.Value = Place(main);
            main  += MainExtent(size);
            cross  = MathF.Max(cross, CrossExtent(size));
        }
        return constraints.Constrain(BuildSize(main, cross));
    }

    /// <summary>Relative position of a child whose main-axis offset is <paramref name="mainOffset"/>.</summary>
    private protected abstract Vector2 Place(float mainOffset);

    /// <summary>The size's extent along the stacking (main) axis.</summary>
    private protected abstract float MainExtent(Vector2 size);

    /// <summary>The size's extent along the cross axis.</summary>
    private protected abstract float CrossExtent(Vector2 size);

    /// <summary>Builds a size from its main-axis and cross-axis extents.</summary>
    private protected abstract Vector2 BuildSize(float main, float cross);
}

/// <summary>
/// Stacks its children vertically, top to bottom — height drives the stacking,
/// the container's width is the widest child (no stretch on this étage).
/// </summary>
public sealed class Column : LinearContainer
{
    private protected override Vector2 Place(float mainOffset) => new(0f, mainOffset);

    private protected override float MainExtent(Vector2 size) => size.Y;

    private protected override float CrossExtent(Vector2 size) => size.X;

    private protected override Vector2 BuildSize(float main, float cross) => new(cross, main);
}

/// <summary>
/// Stacks its children horizontally, left to right — width drives the stacking,
/// the container's height is the tallest child (no stretch on this étage).
/// </summary>
public sealed class Row : LinearContainer
{
    private protected override Vector2 Place(float mainOffset) => new(mainOffset, 0f);

    private protected override float MainExtent(Vector2 size) => size.X;

    private protected override float CrossExtent(Vector2 size) => size.Y;

    private protected override Vector2 BuildSize(float main, float cross) => new(main, cross);
}
