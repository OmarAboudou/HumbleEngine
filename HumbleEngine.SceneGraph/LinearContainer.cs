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

    /// <summary>Adds a non-flex child (content-sized) — sugar for <c>Children.Add</c>.</summary>
    public void Add(UINode child) => Children.Add(child);

    /// <summary>
    /// Adds a child that fills its share of the free main-axis space
    /// (<see cref="Expanded"/>): the descriptor sets the child's
    /// <see cref="UINode.FlexFactor"/>/<see cref="UINode.FlexTight"/>, no extra node
    /// enters the tree.
    /// </summary>
    public void Add(Expanded child)
    {
        Children.Add(child.Child);
        child.Child.FlexFactor.Value = child.Factor;
        child.Child.FlexTight.Value  = true;
    }

    /// <summary>Adds a child that may be smaller than its share (<see cref="Flexible"/>, loose fit).</summary>
    public void Add(Flexible child)
    {
        Children.Add(child.Child);
        child.Child.FlexFactor.Value = child.Factor;
        child.Child.FlexTight.Value  = false;
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
        var count   = Children.Count;
        var spacing = Spacing.Value;
        var mainMax  = MainMax(constraints);
        var crossMax = CrossMax(constraints);
        var finiteMain = !float.IsPositiveInfinity(mainMax);   // flex needs a bounded main axis
        var fillCross  = !float.IsPositiveInfinity(crossMax);  // can only fill a bounded cross axis

        // Pass 1 — size the non-flex children (content), sum the used main space and the
        // total flex factor. A child is flex only when the main axis is bounded.
        var usedMain     = count > 0 ? spacing * (count - 1) : 0f;
        var totalFactor  = 0f;
        var contentCross = 0f;
        for (var i = 0; i < count; i++)
        {
            var child  = Children[i];
            var factor = finiteMain ? FactorOf(child) : 0f;
            if (factor > 0f)
            {
                totalFactor += factor;
                continue; // sized in pass 2, from the leftover space
            }
            child.Incoming.Value = ChildConstraints(0f, mainMax, fillCross, crossMax);
            var size = child.Size.Value;
            usedMain    += MainExtent(size);
            contentCross = MathF.Max(contentCross, CrossExtent(size));
        }

        // Pass 2 — share the free main space among the flex children (∝ factor).
        var free = finiteMain ? MathF.Max(0f, mainMax - usedMain) : 0f;
        for (var i = 0; i < count; i++)
        {
            var child  = Children[i];
            var factor = finiteMain ? FactorOf(child) : 0f;
            if (factor <= 0f)
                continue;
            var share   = totalFactor > 0f ? free * (factor / totalFactor) : 0f;
            var mainMin = TightOf(child) ? share : 0f;   // Expanded fills its share; Flexible may shrink
            child.Incoming.Value = ChildConstraints(mainMin, share, fillCross, crossMax);
            contentCross = MathF.Max(contentCross, CrossExtent(child.Size.Value));
        }

        // Place every child along the main axis, summing the real extents.
        var offset    = 0f;
        var totalMain = 0f;
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                offset    += spacing;
                totalMain += spacing;
            }
            var child = Children[i];
            child.Position.Value = Place(offset);
            var m = MainExtent(child.Size.Value);
            offset    += m;
            totalMain += m;
        }

        var crossExtent = fillCross ? crossMax : contentCross;
        return constraints.Constrain(BuildSize(totalMain, crossExtent));
    }

    private static float FactorOf(UINode child) => child.FlexFactor.Value;

    private static bool TightOf(UINode child) => child.FlexTight.Value;

    // Builds a child constraint from main-axis bounds; the cross axis is tight to
    // crossMax when filling (stretch), loose otherwise (content).
    private Constraints ChildConstraints(float mainMin, float mainMax, bool fillCross, float crossMax) =>
        BuildConstraints(mainMin, mainMax, fillCross ? crossMax : 0f, crossMax);

    /// <summary>Relative position of a child whose main-axis offset is <paramref name="mainOffset"/>.</summary>
    private protected abstract Vector2 Place(float mainOffset);

    /// <summary>The size's extent along the stacking (main) axis.</summary>
    private protected abstract float MainExtent(Vector2 size);

    /// <summary>The size's extent along the cross axis.</summary>
    private protected abstract float CrossExtent(Vector2 size);

    /// <summary>Builds a size from its main-axis and cross-axis extents.</summary>
    private protected abstract Vector2 BuildSize(float main, float cross);

    /// <summary>The constraint's maximum along the main (stacking) axis.</summary>
    private protected abstract float MainMax(Constraints constraints);

    /// <summary>The constraint's maximum along the cross axis.</summary>
    private protected abstract float CrossMax(Constraints constraints);

    /// <summary>Builds a child constraint from per-axis main/cross bounds.</summary>
    private protected abstract Constraints BuildConstraints(float mainMin, float mainMax, float crossMin, float crossMax);
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

    private protected override float MainMax(Constraints constraints) => constraints.MaxHeight;

    private protected override float CrossMax(Constraints constraints) => constraints.MaxWidth;

    private protected override Constraints BuildConstraints(float mainMin, float mainMax, float crossMin, float crossMax) =>
        new(crossMin, crossMax, mainMin, mainMax);
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

    private protected override float MainMax(Constraints constraints) => constraints.MaxWidth;

    private protected override float CrossMax(Constraints constraints) => constraints.MaxHeight;

    private protected override Constraints BuildConstraints(float mainMin, float mainMax, float crossMin, float crossMax) =>
        new(mainMin, mainMax, crossMin, crossMax);
}
