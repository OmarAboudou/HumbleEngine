namespace HumbleEngine;

/// <summary>Base type for all surface creation descriptors.</summary>
public record SurfaceDescription;

/// <summary>Parameters for creating a desktop window.</summary>
/// <param name="Title">Text displayed in the title bar.</param>
/// <param name="Width">Initial width in pixels.</param>
/// <param name="Height">Initial height in pixels.</param>
/// <param name="Resizable">Whether the user can resize the window.</param>
/// <param name="Fullscreen">Opens the window in fullscreen mode immediately.</param>
public record WindowDescription(
    string Title,
    int Width,
    int Height,
    bool Resizable  = true,
    bool Fullscreen = false
) : SurfaceDescription;

/// <summary>Parameters for creating a mobile surface.</summary>
/// <param name="LockOrientation">Locks the orientation to the initial portrait or landscape state.</param>
public record MobileSurfaceDescription(
    bool LockOrientation = false
) : SurfaceDescription;
