namespace HumbleEngine;

/// <summary>
/// The tree's input machinery, behind <see cref="SceneTree.RouteInput"/> —
/// same altitude as the dispose queue. Routes positional events to the
/// hit-test target and bubbles them up the <see cref="UINode"/> ancestors
/// until consumed; synthesizes per-node hover; holds the implicit pointer
/// capture.
/// <para>
/// All state is <b>per pointing source</b>: events carrying their device
/// (<see cref="IDeviceEvent{TDevice}"/>, covariant) are keyed by it, so two
/// mice — exotic on platforms, trivial in tests — get independent hover and
/// capture. Deviceless events share one default source.
/// </para>
/// </summary>
internal sealed class InputRouter
{
    private sealed class PointerState
    {
        /// <summary>Node currently under this pointer (hover).</summary>
        public UINode? Hovered;

        /// <summary>
        /// Node holding the implicit capture: from a press until the release,
        /// every event of this pointer goes to it, even outside its rect —
        /// without this, no correct button and no drag.
        /// </summary>
        public UINode? Captured;

        /// <summary>
        /// Last known position, while the pointer is over the surface — what
        /// <see cref="RefreshHover"/> re-hit-tests when the <i>world</i> moves
        /// under a still pointer.
        /// </summary>
        public Vector2? LastPosition;
    }

    private static readonly object DefaultSource = new();
    private readonly Dictionary<object, PointerState> _pointers = [];

    /// <summary>Routes one event into the tree below <paramref name="root"/>.</summary>
    public void Route(Node? root, InputEvent inputEvent)
    {
        var state = StateFor(inputEvent);

        // The pointer left the surface: hover ends, capture survives (the
        // platform keeps streaming to us during an implicit grab).
        if (inputEvent is PointerExited)
        {
            state.LastPosition = null;
            SetHovered(state, null, default);
            return;
        }

        if (inputEvent is not PointerEvent pointerEvent || root is null)
            return;

        state.LastPosition = pointerEvent.Position;
        var target = Alive(state.Captured) ?? root.HitTest(pointerEvent.Position);

        switch (pointerEvent)
        {
            case PointerEntered:
                // Window-scoped enter: only refreshes hover; nodes get their
                // synthesized, node-scoped notification below.
                if (state.Captured is null)
                    SetHovered(state, target, pointerEvent.Position);
                return;

            case PointerMoved:
                // Hover follows the hit-test, frozen while captured.
                if (state.Captured is null)
                    SetHovered(state, target, pointerEvent.Position);
                Bubble(target, pointerEvent);
                return;

            case PointerPressed:
                state.Captured ??= target;
                Bubble(target, pointerEvent);
                return;

            case PointerReleased:
                Bubble(target, pointerEvent);
                // Capture ends on release; hover resumes from reality.
                if (state.Captured is not null)
                {
                    state.Captured = null;
                    SetHovered(state, root.HitTest(pointerEvent.Position), pointerEvent.Position);
                }
                return;

            default:
                Bubble(target, pointerEvent);
                return;
        }
    }

    /// <summary>
    /// Re-evaluates hover for every pointer whose position is known: hover is
    /// <b>derived state</b> — pointer position × tree geometry — and Moved
    /// events only report the first half changing. The world moves too (layout,
    /// animations): the tree calls this once per frame, when geometry is
    /// settled. Frozen while captured, like every hover update.
    /// </summary>
    public void RefreshHover(Node? root)
    {
        if (root is null)
            return;
        foreach (var state in _pointers.Values)
        {
            if (state.LastPosition is not { } position || state.Captured is not null)
                continue;
            SetHovered(state, root.HitTest(position), position);
        }
    }

    /// <summary>
    /// Offers the event to the target, then to its <see cref="UINode"/>
    /// ancestors (non-UI nodes are transparent), until one consumes it.
    /// </summary>
    private static void Bubble(UINode? target, InputEvent inputEvent)
    {
        for (Node? node = target; node is not null; node = node.Parent)
        {
            if (node is UINode ui && ui.DispatchInput(inputEvent))
                return;
        }
    }

    /// <summary>
    /// Hover transition: the departed node receives a node-scoped
    /// <see cref="PointerExited"/>, the arrival a <see cref="PointerEntered"/>
    /// at the pointer's position — synthesized, delivered to that single node,
    /// never bubbled.
    /// </summary>
    private static void SetHovered(PointerState state, UINode? hovered, Vector2 position)
    {
        var previous = Alive(state.Hovered);
        if (ReferenceEquals(previous, hovered))
        {
            state.Hovered = hovered;
            return;
        }

        previous?.DispatchInput(new PointerExited());
        hovered?.DispatchInput(new PointerEntered(position));
        state.Hovered = hovered;
    }

    /// <summary>A node queued for disposal mid-interaction must not keep receiving events.</summary>
    private static UINode? Alive(UINode? node) =>
        node is { IsDisposed: false, IsInTree: true } ? node : null;

    /// <summary>
    /// Per-source state: keyed by the producing device when the event carries
    /// one (covariance makes every <c>IDeviceEvent&lt;T&gt;</c> an
    /// <c>IDeviceEvent&lt;object&gt;</c>), one shared default source otherwise.
    /// </summary>
    private PointerState StateFor(InputEvent inputEvent)
    {
        var key = (inputEvent as IDeviceEvent<object>)?.Device ?? DefaultSource;
        if (!_pointers.TryGetValue(key, out var state))
        {
            state = new PointerState();
            _pointers[key] = state;
        }
        return state;
    }
}
