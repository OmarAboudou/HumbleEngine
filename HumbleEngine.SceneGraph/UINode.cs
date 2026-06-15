namespace HumbleEngine;

/// <summary>
/// Base of the UI family: a visual node living in pixel space, defined by a
/// <see cref="Position"/> relative to its nearest UI ancestors and a
/// <see cref="Size"/>. Draws nothing itself (like Godot's <c>Control</c>) —
/// concrete UI nodes do.
/// <para>
/// Both properties are reactive cells (created through
/// <see cref="Node.CreateProperty{T}"/>, so their bindings die with the node).
/// Reactivity is not what makes the panel move on screen — everything is
/// redrawn every frame, <see cref="VisualNode.OnDraw"/> just reads the current
/// values — the cells are the <b>binding surface</b> (wire a model to the UI)
/// and what the layout containers listen to.
/// </para>
/// </summary>
public abstract class UINode : VisualNode
{
    /// <summary>
    /// Top-left corner in pixels, relative to the parent UI node (Y down).
    /// Written by layout containers; bindable by the application.
    /// </summary>
    public Property<Vector2> Position { get; }

    /// <summary>
    /// Extent in pixels — the <b>result</b> of the layout, written by the node's own
    /// layout computation (see <see cref="ComputeLayout"/>). Read by draw, hit-test
    /// and <see cref="GlobalRect"/>. (During the layout migration some nodes still
    /// set it directly; once migrated, it is layout-owned.)
    /// </summary>
    public Property<Vector2> Size { get; }

    /// <summary>
    /// The constraints the parent imposes — the <b>down channel</b> of the layout
    /// protocol ("constraints down, sizes up"). A parent <see cref="ComputeLayout"/>
    /// writes its children's <see cref="Incoming"/>; each node's own computation reads
    /// it and produces its <see cref="Size"/>. Default <see cref="Constraints.Unbounded"/>
    /// (free), so a node nobody constrains keeps the size it computes for itself.
    /// </summary>
    public Property<Constraints> Incoming { get; }

    /// <summary>
    /// Surface in square pixels, derived from <see cref="Size"/> — a read-only
    /// <see cref="Computed{T}"/>: it recomputes whenever the size changes and
    /// cannot be written. Mostly a worked example of a derived value: the
    /// inspector discovers it like any other <c>IObservableValue</c> and shows it
    /// as a live, read-only field.
    /// </summary>
    public Computed<float> Area { get; }

    /// <summary>
    /// Share of a <see cref="LinearContainer"/>'s free main-axis space this node
    /// claims when it is a flex child. 0 (the default) means <b>not flex</b>: the node
    /// takes its content size. &gt; 0 means it takes a slice of the leftover space
    /// proportional to the factor — read by the parent container's layout, ignored by
    /// any other parent. A layout property of the node, like <see cref="Position"/> and
    /// <see cref="Size"/>: set it directly (<c>node.FlexFactor.Value = 1f</c>) or through
    /// the <see cref="Expanded"/>/<see cref="Flexible"/> sugar at <c>Add</c>.
    /// </summary>
    public Property<float> FlexFactor { get; }

    /// <summary>
    /// When flex (<see cref="FlexFactor"/> &gt; 0), whether the node fills its share
    /// exactly (<c>true</c>, Flutter's <c>Expanded</c>) or may be smaller, capped at the
    /// share (<c>false</c>, the default, Flutter's <c>Flexible</c>). Irrelevant when not
    /// flex. A raw factor set without a descriptor is therefore loose.
    /// </summary>
    public Property<bool> FlexTight { get; }

    /// <summary>
    /// Whether the pointer hit-test can target this node. Default true. Set false to
    /// make it transparent to picking — the click passes through to whatever sits
    /// behind it (a decorative label over a clickable row). This is Godot's
    /// <c>mouse_filter</c> IGNORE; <see cref="OnInput"/>'s bool still decides block
    /// vs bubble for the nodes that <i>are</i> hit.
    /// </summary>
    public bool Hittable { get; set; } = true;

    private bool _layoutWired;

    protected UINode()
    {
        Position   = CreateProperty(Vector2.Zero);
        Size       = CreateProperty(Vector2.Zero);
        Incoming   = CreateProperty(Constraints.Unbounded);
        Area       = CreateComputed(() => Size.Value.X * Size.Value.Y);
        FlexFactor = CreateProperty(0f);
        FlexTight  = CreateProperty(false);
    }

    /// <summary>
    /// Computes this node's size within the parent's <paramref name="constraints"/> —
    /// the "sizes up" half of the protocol. Override to define how the node measures:
    /// a leaf from its requested/content size, a container from its children (posing
    /// their <see cref="Incoming"/> and reading their <see cref="Size"/>). Read the
    /// <paramref name="constraints"/> argument, never this node's own <see cref="Size"/>
    /// (that would feed back). The default honours the node's current <see cref="Size"/>
    /// clamped to the constraint — the behaviour of a not-yet-migrated node.
    /// </summary>
    protected virtual Vector2 ComputeLayout(Constraints constraints) =>
        constraints.Constrain(Reactive.Untrack(() => Size.Value));

    /// <summary>
    /// Wires the node's layout computation when it enters a tree: one
    /// <see cref="Effect"/> that reads <see cref="Incoming"/> and writes
    /// <see cref="Size"/> = <see cref="ComputeLayout"/>. Re-runs only when the
    /// constraint changes; skip-unchanged is free (cells notify on real change only).
    /// Created here (not in the constructor) so the node is fully built — overrides of
    /// <see cref="ComputeLayout"/> may read subclass fields. Once, guarded against
    /// re-attach. Subclasses overriding <see cref="OnAttached"/> must call base.
    /// </summary>
    protected override void OnAttached()
    {
        base.OnAttached();
        if (_layoutWired) return;
        _layoutWired = true;
        CreateEffect(() => Size.Value = ComputeLayout(Incoming.Value));
    }

    /// <summary>
    /// Rectangle in window coordinates: <see cref="Position"/> offset by every
    /// UI ancestor's (non-UI nodes in the chain are transparent — they have no
    /// geometry). Resolved on demand, O(depth) — the dirty-flagged cache is
    /// Godot's optimisation, deferred until deep trees exist.
    /// </summary>
    public Rect GlobalRect
    {
        get
        {
            var origin = Position.Value;
            for (var ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent)
            {
                if (ancestor is UINode ui)
                    origin += ui.Position.Value;
            }
            return new Rect(origin, Size.Value);
        }
    }

    /// <summary>
    /// Called for every input event routed to this node: as the hit-test
    /// target or a bubbling ancestor (positional events), or as the single
    /// addressee of a synthesized hover notification
    /// (<see cref="PointerEntered"/>/<see cref="PointerExited"/>, no bubbling).
    /// Return <c>true</c> to consume — the records are immutable, the return
    /// value is the signal that stops the propagation. Never mutate the tree
    /// structure here — destruction goes through <see cref="Node.QueueDispose"/>.
    /// </summary>
    protected virtual bool OnInput(InputEvent inputEvent) => false;

    /// <summary>Router entry point — same internal machinery as the lifecycle hooks.</summary>
    internal bool DispatchInput(InputEvent inputEvent) => OnInput(inputEvent);

    /// <summary>
    /// Takes the keyboard focus of this node's tree: key and text events route
    /// here first, then bubble up. Granted by code on this étage —
    /// click-to-focus arrives with focusable widgets (the text field).
    /// No-op when detached.
    /// </summary>
    public void GrabFocus() => Tree?.SetFocus(this);
}
