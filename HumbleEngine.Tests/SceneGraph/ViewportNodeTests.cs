namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for <see cref="ViewportNode"/>: content adoption/detach/replace,
/// background property, and that the content node becomes a proper child of the
/// viewport in the tree (so its GlobalRect is offset by the viewport's position).
/// </summary>
public sealed class ViewportNodeTests
{
    [Test]
    public void Content_IsNull_Initially()
    {
        var vp = new ViewportNode();

        Assert.That(vp.Content, Is.Null);
    }

    [Test]
    public void SetContent_AttachesNodeAsChild()
    {
        var vp   = new ViewportNode();
        var node = new Panel();

        vp.Content = node;

        Assert.That(node.Parent, Is.SameAs(vp));
        Assert.That(vp.Content, Is.SameAs(node));
    }

    [Test]
    public void SetContent_ToNull_DetachesContent()
    {
        var vp   = new ViewportNode();
        var node = new Panel();
        vp.Content = node;

        vp.Content = null;

        Assert.That(node.Parent, Is.Null);
        Assert.That(vp.Content, Is.Null);
    }

    [Test]
    public void SetContent_ToSecond_DetachesFirst()
    {
        var vp    = new ViewportNode();
        var first = new Panel();
        var second = new Panel();
        vp.Content = first;

        vp.Content = second;

        Assert.That(first.Parent,  Is.Null);
        Assert.That(second.Parent, Is.SameAs(vp));
        Assert.That(vp.Content,    Is.SameAs(second));
    }

    [Test]
    public void Content_GlobalRect_IsOffsetByViewportPosition()
    {
        // Content's GlobalRect should sum the viewport's position.
        var vp   = new ViewportNode();
        vp.Position.Value = new Vector2(100f, 50f);

        var content = new Panel();
        content.Position.Value = new Vector2(10f, 5f);
        vp.Content = content;

        var globalRect = content.GlobalRect;
        Assert.That(globalRect.X, Is.EqualTo(110f).Within(0.01f));
        Assert.That(globalRect.Y, Is.EqualTo(55f).Within(0.01f));
    }

    [Test]
    public void Background_IsObservableValue()
    {
        var vp = new ViewportNode();

        Assert.That(vp.Background, Is.InstanceOf<IObservableValue<Vector4>>());
    }

    [Test]
    public void Background_DefaultIsNotTransparent()
    {
        var vp = new ViewportNode();

        // The default background has a non-zero alpha — the viewport is visible.
        Assert.That(vp.Background.Value.W, Is.GreaterThan(0f));
    }

    [Test]
    public void Background_CanBeChanged()
    {
        var vp = new ViewportNode();
        var red = new Vector4(1f, 0f, 0f, 1f);
        var seen = new List<Vector4>();
        using var effect = new Effect(() => seen.Add(vp.Background.Value));

        vp.Background.Value = red;

        Assert.That(seen.Last(), Is.EqualTo(red));
    }
}
