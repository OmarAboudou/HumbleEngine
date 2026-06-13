namespace HumbleEngine.Tests.Editor;

/// <summary>
/// Unit tests for <see cref="EditorState"/>: the selection starts empty, is a
/// reactive cell other panels can observe, and an <see cref="Effect"/> reading it
/// re-runs when the selection changes.
/// </summary>
public sealed class EditorStateTests
{
    private sealed class TestNode : Node;

    [Test]
    public void Selection_StartsNull()
    {
        var state = new EditorState();

        Assert.That(state.Selection.Value, Is.Null);
    }

    [Test]
    public void Selection_NotifiesObserversOnChange()
    {
        var state = new EditorState();
        var node = new TestNode();
        var seen = new List<Node?>();
        using var effect = new Effect(() => seen.Add(state.Selection.Value));

        state.Selection.Value = node;
        state.Selection.Value = null;

        Assert.That(seen, Is.EqualTo(new Node?[] { null, node, null }));
    }
}
