namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for <see cref="UINode.Hittable"/>: a non-hittable node is transparent
/// to the pointer hit-test (Godot's mouse_filter IGNORE) — the click passes through
/// to whatever sits behind it — while a hittable node is targeted as usual.
/// </summary>
public sealed class HittableTests
{
    private static TestUINode At(string name, List<string> log, float x, float y, float w, float h)
    {
        var node = new TestUINode(name, log);
        node.Position.Value = new Vector2(x, y);
        node.Size.Value = new Vector2(w, h);
        return node;
    }

    [Test]
    public void NonHittableChild_IsTransparent_HitReachesTheRowBehind()
    {
        var log = new List<string>();
        var row = At("row", log, 0f, 0f, 100f, 100f);
        var deco = At("deco", log, 0f, 0f, 100f, 100f);
        deco.Hittable = false;
        row.AttachChild(deco);
        using var tree = new SceneTree(new FakeRenderer()) { Root = row };

        tree.RouteInput(new PointerPressed(PointerButton.Left, new Vector2(50f, 50f)));

        // deco is transparent → the hit lands on row, deco is never visited.
        Assert.That(log, Is.EqualTo(new[] { "row:PointerPressed" }));
    }

    [Test]
    public void HittableChild_IsTargeted_ThenBubbles()
    {
        var log = new List<string>();
        var row = At("row", log, 0f, 0f, 100f, 100f);
        var deco = At("deco", log, 0f, 0f, 100f, 100f); // hittable by default
        row.AttachChild(deco);
        using var tree = new SceneTree(new FakeRenderer()) { Root = row };

        tree.RouteInput(new PointerPressed(PointerButton.Left, new Vector2(50f, 50f)));

        Assert.That(log, Is.EqualTo(new[] { "deco:PointerPressed", "row:PointerPressed" }));
    }
}
