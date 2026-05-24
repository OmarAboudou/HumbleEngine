namespace HumbleEngine;

/// <summary>
/// Espace maximal offert par le parent à un enfant, marges comprises.
/// </summary>
public readonly record struct Constraints(float MaxWidth, float MaxHeight)
{
    public static readonly Constraints Unconstrained = new(float.PositiveInfinity, float.PositiveInfinity);
}
