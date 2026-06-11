namespace HumbleEngine;

public record SurfaceDescription;

public record WindowDescription(
    string Title,
    int Width,
    int Height,
    bool Resizable  = true,
    bool Fullscreen = false
) : SurfaceDescription;

public record MobileSurfaceDescription(
    bool LockOrientation = false
) : SurfaceDescription;
