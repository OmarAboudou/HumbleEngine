namespace HumbleEngine;

public abstract class Window : IWindow
{
    public bool ShouldClose { get; protected set; }

    public event Action? OnClose;
    public event Action<int, int>? OnResize;

    public void Run(Action onFrame)
    {
        while (!ShouldClose)
        {
            PollEvents();
            onFrame();
        }
    }

    // PollEvents est un détail d'implémentation : chaque backend traite sa propre
    // file d'événements natifs. Doit s'exécuter sur le thread principal.
    protected abstract void PollEvents();

    public abstract void Show();
    public abstract void Hide();
    public abstract void SetTitle(string title);
    public abstract void Resize(int width, int height);
    public abstract IWindow CreateChildWindow(WindowDescription description);
    public abstract void Dispose();

    protected void RaiseClose() => OnClose?.Invoke();
    protected void RaiseResize(int width, int height) => OnResize?.Invoke(width, height);
}
