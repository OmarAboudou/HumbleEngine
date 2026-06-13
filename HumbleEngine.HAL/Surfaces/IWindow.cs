namespace HumbleEngine;

/// <summary>
/// Desktop window with controllable title, size, and visibility.
/// Inherits the surface lifecycle from <see cref="IGraphicsSurface"/>.
/// </summary>
public interface IWindow : IGraphicsSurface
{
    /// <summary>
    /// The windowing backend that created this window. Graphics backends use it
    /// to identify the window system (X11, Wayland…) and validate compatibility.
    /// </summary>
    IWindowBackend Backend { get; }

    /// <summary>Current client-area width in pixels. Kept up to date by resize events.</summary>
    int Width { get; }

    /// <summary>Current client-area height in pixels. Kept up to date by resize events.</summary>
    int Height { get; }

    /// <summary>Makes the window visible.</summary>
    void Show();

    /// <summary>Hides the window without destroying it.</summary>
    void Hide();

    /// <summary>Sets the text displayed in the title bar.</summary>
    void SetTitle(string title);

    /// <summary>Resizes the window to the given dimensions in pixels.</summary>
    void Resize(int width, int height);

    /// <summary>Fired when the window is resized; arguments are the new width and height in pixels.</summary>
    event Action<int, int>? OnResize;

    /// <summary>
    /// Drains this window's native event queue once — the loop primitive:
    /// <see cref="IGraphicsSurface.Run"/> is literally
    /// <c>while (!ShouldClose) { PollEvents(); onFrame(); }</c>, and a
    /// multi-window application composes its own loop from this. <b>Must be
    /// called on the main thread</b> (X11, Win32 and Cocoa mandate it) — a
    /// documented contract, by design impossible to enforce structurally.
    /// </summary>
    void PollEvents();

    /// <summary>Creates a child window that shares the parent's display connection.</summary>
    IWindow CreateChildWindow(WindowDescription description);

    /// <summary>
    /// The system clipboard, backed by this window's connection — the application
    /// hands it to a scene tree (<c>tree.Clipboard = window.Clipboard</c>).
    /// </summary>
    IClipboard Clipboard { get; }
}
