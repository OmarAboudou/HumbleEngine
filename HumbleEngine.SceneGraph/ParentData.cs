namespace HumbleEngine;

/// <summary>
/// Opaque per-child layout data a parent attaches to its child — Flutter's
/// <c>ParentData</c>. A parent type declares its own subtype (e.g. a flex weight)
/// by overriding <see cref="Node.CreateParentData"/>; the framework stamps a fresh
/// instance on the child at adoption (when <see cref="Node.Parent"/> is set) and
/// clears it on departure. The child carries the slot (<see cref="Node.ParentData"/>),
/// but the data's meaning belongs to the parent that interprets it.
/// <para>
/// Hold reactive values (a bag of <c>Property</c>) in the subtype so editing them
/// re-runs the layout and the inspector can surface them — see <c>FlexParentData</c>.
/// </para>
/// </summary>
public abstract class ParentData
{
}
