namespace HumbleEngine;

/// <summary>
/// Base of every platform input event, published on the surface's single
/// channel (<see cref="IGraphicsSurface.OnInput"/>) — the backends are the
/// abstraction layer, translating their native dialect into this vocabulary.
/// <para>
/// The vocabulary is <b>device-neutral</b> (the W3C Pointer Events model): a
/// "pointer" is anything that points — a mouse today, a finger or pen when
/// mobile arrives (<c>PointerId</c>/<c>DeviceKind</c> will join then). Same
/// words, different grammars: a mouse is a persistent pointer (it moves
/// without contact — hover); a touch is transient (born at
/// <see cref="PointerPressed"/>, moving means dragging, dead at
/// <see cref="PointerReleased"/>). Never assume a move or an enter precedes
/// a press.
/// </para>
/// <para>
/// Device-specific events <b>derive</b> from the neutral ones and add their
/// device through <see cref="IDeviceEvent{TDevice}"/> (e.g.
/// <see cref="MouseMoved"/> is a <see cref="PointerMoved"/>): one event
/// instance, visible at both altitudes — consumers match the level they
/// care about, nothing is emitted twice.
/// </para>
/// </summary>
public abstract record InputEvent;

/// <summary>
/// Positional half of the vocabulary — what the scene tree hit-tests.
/// <see cref="Position"/> is in surface-local pixels, origin at the top-left,
/// Y down: already the UI space, on every backend.
/// </summary>
public abstract record PointerEvent(Vector2 Position) : InputEvent;

/// <summary>The pointer moved over the surface (mouse: anytime; touch: only while dragging).</summary>
public record PointerMoved(Vector2 Position) : PointerEvent(Position);

/// <summary>A pointer button was pressed (a touch contact began).</summary>
public record PointerPressed(PointerButton Button, Vector2 Position) : PointerEvent(Position);

/// <summary>A pointer button was released (a touch contact ended).</summary>
public record PointerReleased(PointerButton Button, Vector2 Position) : PointerEvent(Position);

/// <summary>
/// The wheel scrolled. <see cref="Delta"/> is in wheel notches: +Y away from
/// the user (scroll up), +X to the right — normalized identically on every
/// backend.
/// </summary>
public record PointerScrolled(Vector2 Delta, Vector2 Position) : PointerEvent(Position);

/// <summary>
/// The pointer entered the surface — or, delivered to a single node by the
/// router, entered that node (hover). Same word, two scopes.
/// </summary>
public record PointerEntered(Vector2 Position) : PointerEvent(Position);

/// <summary>
/// The pointer left the surface (or, node-scoped, left that node — hover end).
/// Not positional: Wayland reports no exit coordinates, and leaving is a
/// state reset, never a hit-test.
/// </summary>
public record PointerExited : InputEvent;

/// <summary>Pointer buttons, W3C order — touch maps to <see cref="Left"/> (the primary contact).</summary>
public enum PointerButton
{
    Left   = 0,
    Middle = 1,
    Right  = 2,
}

// --- Mouse-specific events: the neutral event plus its producing device. ---

/// <inheritdoc cref="PointerMoved"/>
public sealed record MouseMoved(Mouse Device, Vector2 Position)
    : PointerMoved(Position), IDeviceEvent<Mouse>;

/// <inheritdoc cref="PointerPressed"/>
public sealed record MousePressed(Mouse Device, PointerButton Button, Vector2 Position)
    : PointerPressed(Button, Position), IDeviceEvent<Mouse>;

/// <inheritdoc cref="PointerReleased"/>
public sealed record MouseReleased(Mouse Device, PointerButton Button, Vector2 Position)
    : PointerReleased(Button, Position), IDeviceEvent<Mouse>;

/// <inheritdoc cref="PointerScrolled"/>
public sealed record MouseScrolled(Mouse Device, Vector2 Delta, Vector2 Position)
    : PointerScrolled(Delta, Position), IDeviceEvent<Mouse>;

/// <inheritdoc cref="PointerEntered"/>
public sealed record MouseEntered(Mouse Device, Vector2 Position)
    : PointerEntered(Position), IDeviceEvent<Mouse>;

/// <inheritdoc cref="PointerExited"/>
public sealed record MouseExited(Mouse Device)
    : PointerExited, IDeviceEvent<Mouse>;
