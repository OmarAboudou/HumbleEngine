using System.Runtime.InteropServices;

namespace HumbleEngine;

/// <summary>
/// Vertex of the engine's drawing contract: a clip-space position and an RGB
/// colour, interpolated across the primitive. Sequential layout (stride 20,
/// offsets 0 and 8) — backends upload vertices as a raw memory copy, no
/// translation, which is the whole point of Mathematics' verified layouts.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Vertex(Vector2 position, Vector3 color)
{
    /// <summary>Position in the backend's clip space.</summary>
    public readonly Vector2 Position = position;

    /// <summary>RGB colour fed to the colour attribute.</summary>
    public readonly Vector3 Color = color;
}
