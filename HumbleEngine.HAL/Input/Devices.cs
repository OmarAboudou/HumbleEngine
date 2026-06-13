namespace HumbleEngine;

/// <summary>
/// Cross-cutting trait of events that carry their producing device. The
/// single-inheritance slot of records is taken by the event family
/// (<see cref="PointerEvent"/>…), so the interface is the vehicle; matching
/// stays expressive: <c>e is IDeviceEvent&lt;Mouse&gt; m → m.Device</c>.
/// Covariant, so any device event is an <c>IDeviceEvent&lt;object&gt;</c> —
/// the router keys its per-source state through that.
/// </summary>
public interface IDeviceEvent<out TDevice>
{
    /// <summary>The device that produced this event.</summary>
    TDevice Device { get; }
}

/// <summary>
/// Identity token of a pointing source — <b>the finest the platform can
/// distinguish</b>, which varies: X11 core events and the Wayland seat both
/// erase physical provenance (one token per window today); XInput2 and Raw
/// Input will mint one per physical device, Wayland one per seat — without
/// any schema change, the events carry whatever token their backend minted.
/// Reference identity is the identity.
/// </summary>
public sealed class Mouse
{
}

/// <summary>
/// Identity token of a keying source — same best-effort granularity story as
/// <see cref="Mouse"/>: one per window today (X11 core and the Wayland seat
/// merge physical keyboards), finer when a platform exposes it.
/// </summary>
public sealed class Keyboard
{
}
