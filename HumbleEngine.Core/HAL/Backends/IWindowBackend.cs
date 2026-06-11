namespace HumbleEngine;

/// <summary>Desktop windowing backend: creates and manages native windows.</summary>
public interface IWindowBackend : ISurfaceBackend
{
    /// <summary>Creates a native window from the given descriptor.</summary>
    IWindow CreateWindow(WindowDescription description);
}
