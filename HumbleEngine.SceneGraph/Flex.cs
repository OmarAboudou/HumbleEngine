namespace HumbleEngine;

/// <summary>
/// Per-child layout data of a <see cref="LinearContainer"/> — how a child shares the
/// main-axis space. A bag of reactive cells (not a struct), so editing one re-runs the
/// layout, the weight is animable, and the inspector surfaces one row per field.
/// <list type="bullet">
///   <item><see cref="Factor"/> 0 → the child is <b>not</b> flex: it takes its content
///     size (the default for a plainly-added child).</item>
///   <item><see cref="Factor"/> &gt; 0 → the child takes a share of the free space,
///     proportional to the factor. <see cref="Tight"/> decides whether it fills that
///     share exactly (<c>Expanded</c>) or may be smaller (<c>Flexible</c>).</item>
/// </list>
/// </summary>
public sealed class FlexParentData : ParentData
{
    /// <summary>Share of the free main-axis space; 0 means the child is not flex.</summary>
    public Property<float> Factor { get; } = new(0f);

    /// <summary>When flex, whether the child fills its share exactly (vs. may be smaller).</summary>
    public Property<bool> Tight { get; } = new(false);
}

/// <summary>
/// Descriptor — <b>not a node</b> — that makes a child fill its share of a
/// <see cref="LinearContainer"/>'s free main-axis space (Flutter's <c>Expanded</c>).
/// Passed to <see cref="LinearContainer.Add(Expanded)"/>, where it dissolves into the
/// child's <see cref="FlexParentData"/> (no extra node in the tree).
/// </summary>
public readonly struct Expanded
{
    /// <summary>The child to expand (required — an empty descriptor is meaningless).</summary>
    public UINode Child { get; }

    /// <summary>Relative share of the free space (default 1).</summary>
    public float Factor { get; }

    /// <summary>Wraps <paramref name="child"/> with a flex <paramref name="factor"/>.</summary>
    public Expanded(UINode child, float factor = 1f)
    {
        ArgumentNullException.ThrowIfNull(child);
        Child  = child;
        Factor = factor;
    }
}

/// <summary>
/// Descriptor — <b>not a node</b> — like <see cref="Expanded"/> but the child may be
/// <i>smaller</i> than its share (Flutter's <c>Flexible</c>, loose fit): it takes its
/// content size, capped at the share.
/// </summary>
public readonly struct Flexible
{
    /// <summary>The child to flex (required).</summary>
    public UINode Child { get; }

    /// <summary>Relative share of the free space (default 1).</summary>
    public float Factor { get; }

    /// <summary>Wraps <paramref name="child"/> with a loose flex <paramref name="factor"/>.</summary>
    public Flexible(UINode child, float factor = 1f)
    {
        ArgumentNullException.ThrowIfNull(child);
        Child  = child;
        Factor = factor;
    }
}
