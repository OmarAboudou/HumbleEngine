namespace HumbleEngine;

/// <summary>
/// Desktop platform descriptor — extends <see cref="OS"/> with window backend management.
/// </summary>
public abstract class DesktopOS : OS
{
    /// <summary>All window backends available on this platform.</summary>
    public abstract IReadOnlyList<IWindowBackend> AvailableWindowBackends { get; }

    /// <summary>The default window backend for this platform.</summary>
    public abstract IWindowBackend DefaultWindowBackend { get; }

    /// <summary>Returns the window backend with the given name.</summary>
    /// <exception cref="KeyNotFoundException">No backend with that name exists.</exception>
    public IWindowBackend GetWindowBackend(string name) =>
        AvailableWindowBackends.FirstOrDefault(b => b.Name == name)
        ?? throw new KeyNotFoundException($"Backend de fenêtrage '{name}' introuvable.");

    /// <summary>
    /// Creates a window, initialising the backend first if needed.
    /// Uses <see cref="DefaultWindowBackend"/> when no backend is specified.
    /// </summary>
    public IWindow CreateWindow(WindowDescription description, IWindowBackend? backend = null)
    {
        var b = backend ?? DefaultWindowBackend;
        b.Initialize();
        return b.CreateWindow(description);
    }
}
