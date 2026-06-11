namespace HumbleEngine;

/// <summary>
/// Desktop window with controllable title, size, and visibility.
/// Inherits the surface lifecycle from <see cref="IGraphicsSurface"/>.
/// </summary>
public interface IWindow : IGraphicsSurface
{
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

    /// <summary>Creates a child window that shares the parent's display connection.</summary>
    IWindow CreateChildWindow(WindowDescription description);
}
