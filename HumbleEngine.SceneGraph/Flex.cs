namespace HumbleEngine;

/// <summary>
/// Descriptor — <b>not a node</b> — that makes a child fill its share of a
/// <see cref="LinearContainer"/>'s free main-axis space (Flutter's <c>Expanded</c>).
/// Passed to <see cref="LinearContainer.Add(Expanded)"/>, where it sets the child's
/// <see cref="UINode.FlexFactor"/>/<see cref="UINode.FlexTight"/> (no extra node in the
/// tree) — convenience sugar over setting those cells directly.
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
