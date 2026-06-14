namespace HumbleEngine;

/// <summary>
/// The down-channel of the layout protocol: the min/max box a parent imposes on a
/// child. The child computes its <see cref="UINode.Size"/> within these bounds
/// (<see cref="Constrain"/>) and the parent positions it — "constraints down, sizes
/// up". A <b>tight</b> constraint (min == max on an axis) dictates an exact extent;
/// a <b>loose</b> one (min 0) lets the child pick up to the max — that loose case is
/// how a node's intrinsic/content size is measured, so there is no separate
/// "desired size" to persist.
/// <para>
/// Immutable value type with structural equality, so a <c>Property&lt;Constraints&gt;</c>
/// only notifies when the bounds actually change — the engine behind the layout's
/// skip-unchanged. Y direction stays a consumer convention (the UI layer is Y-down).
/// An unbounded max is <see cref="float.PositiveInfinity"/>.
/// </para>
/// </summary>
public readonly struct Constraints : IEquatable<Constraints>
{
    /// <summary>Smallest allowed width.</summary>
    public float MinWidth { get; }

    /// <summary>Largest allowed width (<see cref="float.PositiveInfinity"/> when unbounded).</summary>
    public float MaxWidth { get; }

    /// <summary>Smallest allowed height.</summary>
    public float MinHeight { get; }

    /// <summary>Largest allowed height (<see cref="float.PositiveInfinity"/> when unbounded).</summary>
    public float MaxHeight { get; }

    /// <summary>Creates a constraint from its four bounds. Callers keep min ≤ max on each axis.</summary>
    public Constraints(float minWidth, float maxWidth, float minHeight, float maxHeight)
    {
        MinWidth  = minWidth;
        MaxWidth  = maxWidth;
        MinHeight = minHeight;
        MaxHeight = maxHeight;
    }

    /// <summary>A tight constraint: the child must take exactly <paramref name="size"/>.</summary>
    public static Constraints Tight(Vector2 size) => new(size.X, size.X, size.Y, size.Y);

    /// <summary>A tight constraint from explicit dimensions.</summary>
    public static Constraints Tight(float width, float height) => new(width, width, height, height);

    /// <summary>A loose constraint: from zero up to <paramref name="max"/> on each axis.</summary>
    public static Constraints Loose(Vector2 max) => new(0f, max.X, 0f, max.Y);

    /// <summary>The unbounded constraint: zero minimum, infinite maximum on both axes.</summary>
    public static Constraints Unbounded =>
        new(0f, float.PositiveInfinity, 0f, float.PositiveInfinity);

    /// <summary>Minimum corner as a vector.</summary>
    public Vector2 Min => new(MinWidth, MinHeight);

    /// <summary>Maximum corner as a vector (components may be infinite).</summary>
    public Vector2 Max => new(MaxWidth, MaxHeight);

    /// <summary>True when both axes are tight (min == max) — the child has no freedom.</summary>
    public bool IsTight => MinWidth == MaxWidth && MinHeight == MaxHeight;

    /// <summary>
    /// The nearest size to <paramref name="size"/> that satisfies these bounds —
    /// each axis clamped to its [min, max]. The child's chosen size passes through
    /// here before becoming its <see cref="UINode.Size"/>.
    /// </summary>
    public Vector2 Constrain(Vector2 size) =>
        new(Clamp(size.X, MinWidth, MaxWidth), Clamp(size.Y, MinHeight, MaxHeight));

    /// <summary>
    /// A copy with the minimums dropped to zero, the maximums kept. Turning a tight
    /// constraint loose is how a parent measures a child's intrinsic size before
    /// deciding its final allocation.
    /// </summary>
    public Constraints Loosen() => new(0f, MaxWidth, 0f, MaxHeight);

    // Manual clamp (not Math.Clamp) so an infinite max never trips the min ≤ max check.
    private static float Clamp(float value, float min, float max) =>
        MathF.Min(MathF.Max(value, min), max);

    /// <summary>Exact component equality. Beware float rounding on computed results.</summary>
    public static bool operator ==(Constraints a, Constraints b) => a.Equals(b);

    /// <summary>Exact component inequality.</summary>
    public static bool operator !=(Constraints a, Constraints b) => !a.Equals(b);

    /// <inheritdoc />
    public bool Equals(Constraints other) =>
        MinWidth == other.MinWidth && MaxWidth == other.MaxWidth &&
        MinHeight == other.MinHeight && MaxHeight == other.MaxHeight;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Constraints other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(MinWidth, MaxWidth, MinHeight, MaxHeight);

    /// <inheritdoc />
    public override string ToString() => $"[{MinWidth}..{MaxWidth}] × [{MinHeight}..{MaxHeight}]";
}
