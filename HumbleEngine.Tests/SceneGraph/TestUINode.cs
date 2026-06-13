namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Concrete <see cref="UINode"/> for the routing tests: exposes the protected
/// composition API, records every <see cref="UINode.OnInput"/> call into a
/// shared ordered log as <c>name:EventTypeName</c>, and consumes according to
/// an optional handler (default: consumes nothing, everything bubbles).
/// </summary>
internal sealed class TestUINode(string name, List<string>? log = null) : UINode
{
    /// <summary>Decides consumption per event; null consumes nothing.</summary>
    public Func<InputEvent, bool>? InputHandler { get; set; }

    public void AttachChild(Node child) => Attach(child);

    protected override bool OnInput(InputEvent inputEvent)
    {
        log?.Add($"{name}:{inputEvent.GetType().Name}");
        return InputHandler?.Invoke(inputEvent) ?? false;
    }
}
