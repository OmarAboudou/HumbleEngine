namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for <see cref="UINode"/> and <see cref="Panel"/>: global-rect
/// resolution through the ancestor chain (UI ancestors offset, non-UI ones are
/// transparent), the panel's draw, and the cells as binding surface.
/// </summary>
public sealed class UINodeTests
{
    /// <summary>Concrete <see cref="UINode"/> opening its composition for the nesting tests.</summary>
    private sealed class ContainerUINode : UINode
    {
        public void AttachChild(Node child) => Attach(child);
    }

    private static Panel MakePanel(float x, float y, float width, float height)
    {
        var panel = new Panel();
        panel.Position.Value = new Vector2(x, y);
        panel.Size.Value     = new Vector2(width, height);
        return panel;
    }

    [Test]
    public void GlobalRect_WithoutUIAncestors_IsPositionPlusSize()
    {
        var panel = MakePanel(10f, 20f, 30f, 40f);

        Assert.That(panel.GlobalRect, Is.EqualTo(new Rect(10f, 20f, 30f, 40f)));
    }

    [Test]
    public void GlobalRect_AccumulatesUIAncestorsOffsets()
    {
        var outer = new ContainerUINode();
        outer.Position.Value = new Vector2(100f, 50f);
        var inner = new ContainerUINode();
        inner.Position.Value = new Vector2(10f, 5f);
        var panel = MakePanel(1f, 2f, 30f, 40f);
        outer.AttachChild(inner);
        inner.AttachChild(panel);

        Assert.That(panel.GlobalRect, Is.EqualTo(new Rect(111f, 57f, 30f, 40f)));
    }

    [Test]
    public void GlobalRect_NonUIAncestors_AreTransparent()
    {
        var ui = new ContainerUINode();
        ui.Position.Value = new Vector2(5f, 7f);
        var logic = new TestNode("logic");
        var panel = MakePanel(1f, 2f, 30f, 40f);
        ui.AttachChild(logic);
        logic.AttachChild(panel);

        Assert.That(panel.GlobalRect, Is.EqualTo(new Rect(6f, 9f, 30f, 40f)));
    }

    [Test]
    public void Panel_DrawsItsGlobalRect_WithItsColor()
    {
        var panel = MakePanel(10f, 20f, 30f, 40f);
        panel.Color.Value = new Vector4(0.1f, 0.2f, 0.3f, 0.4f);
        var renderer = new FakeRenderer();
        using var tree = new SceneTree(renderer) { Root = panel };

        tree.Render();

        Assert.That(renderer.Quads, Has.Count.EqualTo(1));
        Assert.That(renderer.Quads[0].Rect, Is.EqualTo(new Rect(10f, 20f, 30f, 40f)));
        Assert.That(renderer.Quads[0].Color, Is.EqualTo(new Vector4(0.1f, 0.2f, 0.3f, 0.4f)));
    }

    [Test]
    public void Position_IsABindingSurface()
    {
        var panel = MakePanel(0f, 0f, 10f, 10f);
        var source = new Property<Vector2>(new Vector2(5f, 5f));

        panel.Position.BindFrom(source);
        source.Value = new Vector2(30f, 40f);

        Assert.That(panel.GlobalRect.Position, Is.EqualTo(new Vector2(30f, 40f)));
    }

    [Test]
    public void DisposingThePanel_ReleasesItsBindings()
    {
        var panel = MakePanel(0f, 0f, 10f, 10f);
        var source = new Property<Vector2>(Vector2.Zero);
        panel.Position.BindFrom(source);

        panel.Dispose();

        Assert.That(panel.Position.IsBound, Is.False);
    }
}
