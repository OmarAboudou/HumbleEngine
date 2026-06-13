namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for <see cref="SceneTree.RouteInput"/>: reverse-painter
/// hit-testing, boolean-return bubbling through UI ancestors (non-UI nodes
/// transparent), implicit pointer capture, synthesized per-node hover —
/// including the per-frame refresh when the world moves under a still pointer
/// — keyboard focus, and the per-source state that gives two phantom mice
/// independent hover and capture without any platform support.
/// </summary>
public sealed class InputRouterTests
{
    private static TestUINode MakeNode(string name, List<string> log, float x, float y, float width = 100f, float height = 100f)
    {
        var node = new TestUINode(name, log);
        node.Position.Value = new Vector2(x, y);
        node.Size.Value     = new Vector2(width, height);
        return node;
    }

    [Test]
    public void HitTest_TopmostWins_AndBubblesToUIAncestorsOnly()
    {
        var log = new List<string>();
        var root = new TestNode("root");
        var under = MakeNode("under", log, 0f, 0f);
        var over  = MakeNode("over", log, 0f, 0f); // same rect, attached later: drawn later, on top
        root.AttachChild(under);
        root.AttachChild(over);
        using var tree = new SceneTree(new FakeRenderer()) { Root = root };

        tree.RouteInput(new PointerPressed(PointerButton.Left, new Vector2(50f, 50f)));

        // "over" wins the hit-test; "under" is a sibling, not an ancestor — it sees nothing.
        Assert.That(log, Is.EqualTo(new[] { "over:PointerPressed" }));
    }

    [Test]
    public void Bubbling_ClimbsUIAncestors_SkippingLogicNodes_UntilConsumed()
    {
        var log = new List<string>();
        var top = MakeNode("top", log, 0f, 0f, 400f, 400f);
        var logic = new TestNode("logic");
        var leaf = MakeNode("leaf", log, 10f, 10f);
        top.AttachChild(logic);
        logic.AttachChild(leaf);
        using var tree = new SceneTree(new FakeRenderer()) { Root = top };

        tree.RouteInput(new PointerPressed(PointerButton.Left, new Vector2(50f, 50f)));
        Assert.That(log, Is.EqualTo(new[] { "leaf:PointerPressed", "top:PointerPressed" }));

        log.Clear();
        leaf.InputHandler = e => e is PointerPressed;
        tree.RouteInput(new PointerReleased(PointerButton.Left, new Vector2(50f, 50f)));
        tree.RouteInput(new PointerPressed(PointerButton.Left, new Vector2(50f, 50f)));

        // The consumed press stops at the leaf; the release before it bubbled.
        Assert.That(log, Does.Contain("top:PointerReleased"));
        Assert.That(log.Count(entry => entry == "top:PointerPressed"), Is.Zero);
    }

    [Test]
    public void ImplicitCapture_RoutesMovesAndReleaseToThePressedNode()
    {
        var log = new List<string>();
        var root = new TestNode("root");
        var a = MakeNode("a", log, 0f, 0f);
        var b = MakeNode("b", log, 200f, 0f);
        a.InputHandler = _ => true; // consume everything: quieter log
        root.AttachChild(a);
        root.AttachChild(b);
        using var tree = new SceneTree(new FakeRenderer()) { Root = root };

        tree.RouteInput(new PointerPressed(PointerButton.Left, new Vector2(50f, 50f)));
        tree.RouteInput(new PointerMoved(new Vector2(250f, 50f)));   // over b, but a captured
        tree.RouteInput(new PointerReleased(PointerButton.Left, new Vector2(250f, 50f)));
        tree.RouteInput(new PointerMoved(new Vector2(250f, 50f)));   // capture ended: b again

        Assert.That(log, Is.EqualTo(new[]
        {
            "a:PointerPressed",
            "a:PointerMoved",
            "a:PointerReleased",
            "b:PointerEntered", // hover resumes from reality at release
            "b:PointerMoved",
        }));
    }

    [Test]
    public void Hover_EnterAndExit_AreNodeScoped_AndDoNotBubble()
    {
        var log = new List<string>();
        var root = new TestNode("root");
        var parent = MakeNode("parent", log, 0f, 0f, 400f, 400f);
        var child = MakeNode("child", log, 0f, 0f);
        parent.AttachChild(child);
        root.AttachChild(parent);
        using var tree = new SceneTree(new FakeRenderer()) { Root = root };

        tree.RouteInput(new PointerMoved(new Vector2(50f, 50f)));   // over child
        tree.RouteInput(new PointerMoved(new Vector2(250f, 250f))); // over parent only

        Assert.That(log, Is.EqualTo(new[]
        {
            "child:PointerEntered",          // never on parent: hover does not bubble
            "child:PointerMoved",
            "parent:PointerMoved",           // the move itself bubbles
            "child:PointerExited",
            "parent:PointerEntered",
            "parent:PointerMoved",
        }));
    }

    [Test]
    public void RefreshHover_FollowsTheWorld_UnderAStillPointer()
    {
        var log = new List<string>();
        var root = new TestNode("root");
        var node = MakeNode("node", log, 0f, 0f);
        root.AttachChild(node);
        using var tree = new SceneTree(new FakeRenderer()) { Root = root };

        tree.RouteInput(new PointerMoved(new Vector2(150f, 50f))); // empty space
        Assert.That(log, Is.Empty);

        node.Position.Value = new Vector2(100f, 0f); // now under the still pointer
        tree.Render();
        Assert.That(log, Is.EqualTo(new[] { "node:PointerEntered" }));

        node.Position.Value = new Vector2(300f, 0f); // slides away
        tree.Render();
        Assert.That(log, Is.EqualTo(new[] { "node:PointerEntered", "node:PointerExited" }));
    }

    [Test]
    public void TwoPhantomMice_HaveIndependentHoverAndCapture()
    {
        var log = new List<string>();
        var mouseA = new Mouse();
        var mouseB = new Mouse();
        var root = new TestNode("root");
        var left  = MakeNode("left", log, 0f, 0f);
        var right = MakeNode("right", log, 200f, 0f);
        left.InputHandler = _ => true;
        root.AttachChild(left);
        root.AttachChild(right);
        using var tree = new SceneTree(new FakeRenderer()) { Root = root };

        // Each mouse hovers its own node: no exit fired for the other.
        tree.RouteInput(new MouseMoved(mouseA, new Vector2(50f, 50f)));
        tree.RouteInput(new MouseMoved(mouseB, new Vector2(250f, 50f)));
        Assert.That(log, Does.Not.Contain("left:PointerExited"));
        Assert.That(log, Does.Contain("left:PointerEntered"));
        Assert.That(log, Does.Contain("right:PointerEntered"));
        log.Clear();

        // Mouse A captures "left"; mouse B keeps free hit-testing.
        tree.RouteInput(new MousePressed(mouseA, PointerButton.Left, new Vector2(50f, 50f)));
        tree.RouteInput(new MouseMoved(mouseA, new Vector2(250f, 50f))); // captured by left
        tree.RouteInput(new MouseMoved(mouseB, new Vector2(250f, 50f))); // free, hits right

        // The bubbled events keep their concrete device type (the bloc-4 "one
        // event, two altitudes" pattern); the synthesized hover above is
        // always the neutral PointerEntered/Exited.
        Assert.That(log, Is.EqualTo(new[]
        {
            "left:MousePressed",
            "left:MouseMoved",
            "right:MouseMoved",
        }));
    }

    [Test]
    public void KeyboardFamily_GoesToTheFocusedNode_AndBubbles()
    {
        var log = new List<string>();
        var root = new TestNode("root");
        var parent = MakeNode("parent", log, 0f, 0f, 400f, 400f);
        var child = MakeNode("child", log, 0f, 0f);
        parent.AttachChild(child);
        root.AttachChild(parent);
        using var tree = new SceneTree(new FakeRenderer()) { Root = root };

        // No focus: keys go nowhere, silently.
        tree.RouteInput(new KeyPressed(Key.A, KeyModifiers.None));
        Assert.That(log, Is.Empty);

        child.GrabFocus();
        Assert.That(tree.FocusedNode, Is.SameAs(child));

        tree.RouteInput(new KeyPressed(Key.A, KeyModifiers.None));
        tree.RouteInput(new TextInput("a"));

        Assert.That(log, Is.EqualTo(new[]
        {
            "child:KeyPressed", "parent:KeyPressed",
            "child:TextInput", "parent:TextInput",
        }));
    }

    [Test]
    public void KeyboardEvents_AreNeverHitTested()
    {
        var log = new List<string>();
        var root = new TestNode("root");
        var hovered = MakeNode("hovered", log, 0f, 0f);
        var focused = MakeNode("focused", log, 200f, 0f);
        root.AttachChild(hovered);
        root.AttachChild(focused);
        using var tree = new SceneTree(new FakeRenderer()) { Root = root };

        tree.RouteInput(new PointerMoved(new Vector2(50f, 50f))); // pointer over "hovered"
        focused.GrabFocus();
        log.Clear();

        tree.RouteInput(new KeyPressed(Key.Enter, KeyModifiers.None));

        Assert.That(log, Is.EqualTo(new[] { "focused:KeyPressed" }));
    }

    [Test]
    public void DeadNodes_StopReceiving_FocusAndCapture()
    {
        var log = new List<string>();
        var root = new TestNode("root");
        var node = MakeNode("node", log, 0f, 0f);
        root.AttachChild(node);
        using var tree = new SceneTree(new FakeRenderer()) { Root = root };

        node.GrabFocus();
        tree.RouteInput(new PointerPressed(PointerButton.Left, new Vector2(50f, 50f))); // captured
        log.Clear();

        node.QueueDispose();
        tree.FlushDisposeQueue();

        Assert.That(tree.FocusedNode, Is.Null);
        tree.RouteInput(new KeyPressed(Key.A, KeyModifiers.None));
        tree.RouteInput(new PointerMoved(new Vector2(50f, 50f)));
        Assert.That(log, Is.Empty);
    }
}
