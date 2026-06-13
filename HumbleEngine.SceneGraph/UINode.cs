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

    /// <summary>Extent in pixels. The layout reads it; never writes it (this étage).</summary>
    public Property<Vector2> Size { get; }

    protected UINode()
    {
        Position = CreateProperty(Vector2.Zero);
        Size     = CreateProperty(Vector2.Zero);
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
