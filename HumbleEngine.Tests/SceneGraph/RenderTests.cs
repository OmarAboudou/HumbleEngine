namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for the tree-driven drawing (roadmaps 07 and 09): traversal
/// order, pure-logic nodes staying silent, the window–renderer–tree trio
/// (the tree's renderer is the one handed to nodes, and the
/// <see cref="VisualNode"/> protocol resolves it), the attach/detach resource
/// lifecycle, and the frame-N death semantics of <see cref="Node.QueueDispose"/>
/// seen from the drawing side.
/// </summary>
public sealed class RenderTests
{
    /// <summary>
    /// The resource-owning node pattern (the Sandbox triangle's shape):
    /// default-constructible, acquires its mesh from the context renderer on
    /// attach, releases it on detach.
    /// </summary>
    private sealed class ResourceNode : VisualNode
    {
        /// <summary>The mesh acquired by the last attach; kept after release for assertions.</summary>
        public FakeMesh? Mesh { get; private set; }

        protected override void OnAttached() =>
            Mesh = (FakeMesh)Renderer!.CreateMesh([new Vertex(default, default)]);

        protected override void OnDetached() => Mesh?.Dispose();
    }

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
        using var tree = new SceneTree(new FakeRenderer()) { Root = root };
        log.Clear();

        tree.Render();

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
        using var tree = new SceneTree(new FakeRenderer()) { Root = root };
        log.Clear();

        tree.Render();

        Assert.That(log, Is.EqualTo(new[] { "visual:Draw" }));
    }

    [Test]
    public void Render_WithoutRoot_DoesNothing()
    {
        using var tree = new SceneTree(new FakeRenderer());

        Assert.DoesNotThrow(() => tree.Render());
    }

    [Test]
    public void Tree_RequiresARenderer()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new SceneTree(null!));
    }

    [Test]
    public void Render_OnDisposedTree_Throws()
    {
        var tree = new SceneTree(new FakeRenderer()) { Root = new TestVisualNode("root") };
        tree.Dispose();

        Assert.Throws<ObjectDisposedException>(() => tree.Render());
    }

    [Test]
    public void Render_HandsTheTreeRendererToNodes()
    {
        var renderer = new FakeRenderer();
        var node = new TestVisualNode("root");
        using var tree = new SceneTree(renderer) { Root = node };

        tree.Render();

        Assert.That(node.LastRenderer, Is.SameAs(renderer));
    }

    [Test]
    public void RendererProtocol_ResolvesTheTreeRenderer_AndNullWhenDetached()
    {
        var renderer = new FakeRenderer();
        var node = new TestVisualNode("node");
        Assert.That(node.RendererView, Is.Null);

        using var tree = new SceneTree(renderer) { Root = node };
        Assert.That(node.RendererView, Is.SameAs(renderer));

        tree.Root = null;
        Assert.That(node.RendererView, Is.Null);
    }

    [Test]
    public void ResourceNode_AcquiresOnAttach_ReleasesOnDetach_ReacquiresOnReattach()
    {
        var renderer = new FakeRenderer();
        var root = new TestNode("root");
        var node = new ResourceNode();
        using var tree = new SceneTree(renderer) { Root = root };

        root.AttachChild(node);
        var firstMesh = node.Mesh;
        Assert.That(firstMesh, Is.Not.Null);
        Assert.That(firstMesh!.IsDisposed, Is.False);

        root.DetachChild(node);
        Assert.That(firstMesh.IsDisposed, Is.True);

        root.AttachChild(node);
        Assert.That(node.Mesh, Is.Not.SameAs(firstMesh));
        Assert.That(node.Mesh!.IsDisposed, Is.False);
    }

    [Test]
    public void ResourceNode_Disposal_ReleasesTheMesh_ThroughTheDetachPath()
    {
        var renderer = new FakeRenderer();
        var root = new TestNode("root");
        var node = new ResourceNode();
        using var tree = new SceneTree(renderer) { Root = root };
        root.AttachChild(node);
        var mesh = node.Mesh!;

        // Dispose detaches first: OnDetached is the single release path.
        node.Dispose();

        Assert.That(mesh.IsDisposed, Is.True);
    }

    [Test]
    public void NodeDrawingItsMesh_ReachesTheRenderer()
    {
        var renderer = new FakeRenderer();
        var mesh = renderer.CreateMesh([new Vertex(new Vector2(0f, 0f), new Vector3(1f, 0f, 0f))]);
        var node = new TestVisualNode("root") { Mesh = mesh };
        using var tree = new SceneTree(renderer) { Root = node };

        tree.Render();

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
        using var tree = new SceneTree(renderer) { Root = root };
        log.Clear();

        // Frame N: the node still draws, then queues its own death.
        tree.Render();
        doomed.QueueDispose();
        Assert.That(log, Does.Contain("doomed:Draw"));
        Assert.That(mesh.IsDisposed, Is.False);

        // End of frame N: the safe point honours the queue, mesh included.
        tree.FlushDisposeQueue();
        Assert.That(mesh.IsDisposed, Is.True);
        log.Clear();

        // Frame N+1: the node is gone from the traversal.
        tree.Render();
        Assert.That(log, Is.EqualTo(new[] { "root:Draw" }));
    }
}
