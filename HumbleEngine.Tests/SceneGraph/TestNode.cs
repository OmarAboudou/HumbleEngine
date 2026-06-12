namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Concrete <see cref="Node"/> for tests: exposes the protected composition API
/// and records lifecycle callbacks, optionally into a shared ordered log.
/// </summary>
internal sealed class TestNode(string name, List<string>? log = null) : Node
{
    /// <summary>Arguments of the last <see cref="Node.OnParentChanged"/> call.</summary>
    public (Node? Old, Node? New)? LastParentChange { get; private set; }

    /// <summary>Number of <see cref="Node.OnParentChanged"/> calls received.</summary>
    public int ParentChangeCount { get; private set; }

    /// <summary>Children, exposed for assertions.</summary>
    public IReadOnlyList<Node> ChildrenView => Children;

    public string NodeName { get; } = name;

    public void AttachChild(Node child) => Attach(child);

    public void DetachChild(Node child) => Detach(child);

    public void ReparentChild(Node child) => Reparent(child);

    protected override void OnParentChanged(Node? oldParent, Node? newParent)
    {
        LastParentChange = (oldParent, newParent);
        ParentChangeCount++;
        log?.Add($"{NodeName}:ParentChanged");
    }

    protected override void OnDispose() => log?.Add($"{NodeName}:Disposed");
}
