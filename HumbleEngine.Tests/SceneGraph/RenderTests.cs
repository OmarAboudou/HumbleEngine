namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for the tree-driven drawing (roadmap 07): traversal order,
/// pure-logic nodes staying silent, the renderer never being retained, and the
/// frame-N death semantics of <see cref="Node.QueueDispose"/> seen from the
/// drawing side — mesh ownership included.
/// </summary>
public sealed class RenderTests
{
    [Test]
    public void Render_VisitsVisualNodes_ParentsBeforeChildren_InAttachOrder()
    {
        var log = new List<string>();
        var root = new TestVisualNode("root", log);
        var left = new TestVisualNode("left", log);
        var right = new TestVisualNode("right", log);
        var leftChild = new TestVisualNode("leftChild", log);
        root.AttachChild(left);
        root.AttachChild(right);
        left.AttachChild(leftChild);
        using var tree = new SceneTree { Root = root };
        log.Clear();

        tree.Render(new FakeRenderer());

        Assert.That(log, Is.EqualTo(new[]
        {
            "root:Draw", "left:Draw", "leftChild:Draw", "right:Draw"
        }));
    }

    [Test]
    public void Render_SkipsPureLogicNodes_ButStillReachesTheirVisualChildren()
    {
        var log = new List<string>();
        var root = new TestNode("root", log);
        var logic = new TestNode("logic", log);
        var visual = new TestVisualNode("visual", log);
        root.AttachChild(logic);
        logic.AttachChild(visual);
        using var tree = new SceneTree { Root = root };
        log.Clear();

        tree.Render(new FakeRenderer());

        Assert.That(log, Is.EqualTo(new[] { "visual:Draw" }));
    }

    [Test]
    public void Render_WithoutRoot_DoesNothing()
    {
        using var tree = new SceneTree();

        Assert.DoesNotThrow(() => tree.Render(new FakeRenderer()));
    }

    [Test]
    public void Render_NullRenderer_Throws()
    {
        using var tree = new SceneTree { Root = new TestVisualNode("root") };

        Assert.Throws<ArgumentNullException>(() => tree.Render(null!));
    }

    [Test]
    public void Render_OnDisposedTree_Throws()
    {
        var tree = new SceneTree { Root = new TestVisualNode("root") };
        tree.Dispose();

        Assert.Throws<ObjectDisposedException>(() => tree.Render(new FakeRenderer()));
    }

    [Test]
    public void Render_HandsTheGivenRendererToNodes_NothingRetained()
    {
        var node = new TestVisualNode("root");
        using var tree = new SceneTree { Root = node };
        var first = new FakeRenderer();
        var second = new FakeRenderer();

        tree.Render(first);
        Assert.That(node.LastRenderer, Is.SameAs(first));

        // A second renderer is honoured as-is: the tree kept no rendering state.
        tree.Render(second);
        Assert.That(node.LastRenderer, Is.SameAs(second));
    }

    [Test]
    public void NodeDrawingItsMesh_ReachesTheRenderer()
    {
        var renderer = new FakeRenderer();
        var mesh = renderer.CreateMesh([new Vertex(new Vector2(0f, 0f), new Vector3(1f, 0f, 0f))]);
        var node = new TestVisualNode("root") { Mesh = mesh };
        using var tree = new SceneTree { Root = node };

        tree.Render(renderer);

        Assert.That(renderer.DrawCount, Is.EqualTo(1));
        Assert.That(renderer.Meshes[0].VertexCount, Is.EqualTo(1));
    }

    [Test]
    public void QueueDispose_DuringFrameN_DrawsInN_GoneWithItsMeshBeforeN1()
    {
        var log = new List<string>();
        var renderer = new FakeRenderer();
        var mesh = (FakeMesh)renderer.CreateMesh([new Vertex(default, default)]);
        var root = new TestVisualNode("root", log);
        var doomed = new TestVisualNode("doomed", log) { Mesh = mesh };
        root.AttachChild(doomed);
        using var tree = new SceneTree { Root = root };
        log.Clear();

        // Frame N: the node still draws, then queues its own death.
        tree.Render(renderer);
        doomed.QueueDispose();
        Assert.That(log, Does.Contain("doomed:Draw"));
        Assert.That(mesh.IsDisposed, Is.False);

        // End of frame N: the safe point honours the queue, mesh included.
        tree.FlushDisposeQueue();
        Assert.That(mesh.IsDisposed, Is.True);
        log.Clear();

        // Frame N+1: the node is gone from the traversal.
        tree.Render(renderer);
        Assert.That(log, Is.EqualTo(new[] { "root:Draw" }));
    }
}
