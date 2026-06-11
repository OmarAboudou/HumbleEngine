namespace HumbleEngine;

public abstract class DesktopOS : OS
{
    public abstract IReadOnlyList<IWindowBackend> AvailableWindowBackends { get; }
    public abstract IWindowBackend DefaultWindowBackend { get; }

    public IWindowBackend GetWindowBackend(string name) =>
        AvailableWindowBackends.FirstOrDefault(b => b.Name == name)
        ?? throw new KeyNotFoundException($"Backend de fenêtrage '{name}' introuvable.");

    public IWindow CreateWindow(WindowDescription description, IWindowBackend? backend = null)
    {
        var b = backend ?? DefaultWindowBackend;
        b.Initialize();
        return b.CreateWindow(description);
    }
}
