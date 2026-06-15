namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for <see cref="Node.Provide{T}"/> / <see cref="Node.Inherit{T}"/> /
/// <see cref="Node.TryInherit{T}"/> — the inherited-context mechanism (anti-prop-drilling).
/// </summary>
public sealed class InheritedContextTests
{
    // ── helpers ─────────────────────────────────────────────────────────────

    private sealed class Token;
    private sealed class OtherToken;

    private static TestNode Chain(int depth)
    {
        var root = new TestNode("root");
        var current = root;
        for (var i = 1; i < depth; i++)
        {
            var next = new TestNode($"node{i}");
            current.AttachChild(next);
            current = next;
        }
        return root;
    }

    // ── Inherit: happy path ──────────────────────────────────────────────────

    [Test]
    public void Inherit_DirectParent_ReturnsValue()
    {
        var parent = new TestNode("parent");
        var child  = new TestNode("child");
        parent.AttachChild(child);

        var token = new Token();
        parent.Provide(token);

        Assert.That(child.Inherit<Token>(), Is.SameAs(token));
    }

    [Test]
    public void Inherit_ThroughIntermediateNodes_ReturnsAncestorValue()
    {
        // grandparent → middle → grandchild
        var grandparent = new TestNode("gp");
        var middle      = new TestNode("mid");
        var grandchild  = new TestNode("gc");
        grandparent.AttachChild(middle);
        middle.AttachChild(grandchild);

        var token = new Token();
        grandparent.Provide(token);

        Assert.That(grandchild.Inherit<Token>(), Is.SameAs(token));
    }

    // ── Shadowing ────────────────────────────────────────────────────────────

    [Test]
    public void Inherit_Shadowing_NearestAncestorWins()
    {
        var grandparent = new TestNode("gp");
        var parent      = new TestNode("parent");
        var child       = new TestNode("child");
        grandparent.AttachChild(parent);
        parent.AttachChild(child);

        var farToken  = new Token();
        var nearToken = new Token();
        grandparent.Provide(farToken);
        parent.Provide(nearToken);

        Assert.That(child.Inherit<Token>(), Is.SameAs(nearToken));
    }

    [Test]
    public void Inherit_DifferentTypes_AreIndependent()
    {
        var parent = new TestNode("parent");
        var child  = new TestNode("child");
        parent.AttachChild(child);

        var token      = new Token();
        var otherToken = new OtherToken();
        parent.Provide(token);
        parent.Provide(otherToken);

        Assert.That(child.Inherit<Token>(),      Is.SameAs(token));
        Assert.That(child.Inherit<OtherToken>(), Is.SameAs(otherToken));
    }

    // ── Not found ────────────────────────────────────────────────────────────

    [Test]
    public void Inherit_NoAncestorProvides_Throws()
    {
        var parent = new TestNode("parent");
        var child  = new TestNode("child");
        parent.AttachChild(child);

        Assert.Throws<InvalidOperationException>(() => child.Inherit<Token>());
    }

    [Test]
    public void Inherit_NoParent_Throws()
    {
        var node = new TestNode("alone");
        Assert.Throws<InvalidOperationException>(() => node.Inherit<Token>());
    }

    [Test]
    public void TryInherit_NoAncestorProvides_ReturnsFalse()
    {
        var parent = new TestNode("parent");
        var child  = new TestNode("child");
        parent.AttachChild(child);

        var found = child.TryInherit<Token>(out var value);

        Assert.That(found, Is.False);
        Assert.That(value, Is.Null);
    }

    [Test]
    public void TryInherit_AncestorProvides_ReturnsTrueAndValue()
    {
        var parent = new TestNode("parent");
        var child  = new TestNode("child");
        parent.AttachChild(child);

        var token = new Token();
        parent.Provide(token);

        var found = child.TryInherit<Token>(out var value);

        Assert.That(found, Is.True);
        Assert.That(value, Is.SameAs(token));
    }

    // ── A node does not inherit its own Provide ──────────────────────────────

    [Test]
    public void Inherit_DoesNotResolveOwnProvide()
    {
        var node = new TestNode("node");
        node.Provide(new Token());

        // Provide targets the subtree; the provider itself cannot inherit its own value.
        Assert.Throws<InvalidOperationException>(() => node.Inherit<Token>());
    }

    // ── Provide overwrites ───────────────────────────────────────────────────

    [Test]
    public void Provide_CalledTwice_OverwritesPreviousValue()
    {
        var parent = new TestNode("parent");
        var child  = new TestNode("child");
        parent.AttachChild(child);

        var first  = new Token();
        var second = new Token();
        parent.Provide(first);
        parent.Provide(second);

        Assert.That(child.Inherit<Token>(), Is.SameAs(second));
    }

    // ── Re-parentage changes resolution ──────────────────────────────────────

    [Test]
    public void Inherit_AfterReparent_UsesNewAncestorChain()
    {
        // tree-A: providerA → child
        var providerA = new TestNode("A");
        var child     = new TestNode("child");
        providerA.AttachChild(child);

        var tokenA = new Token();
        providerA.Provide(tokenA);
        Assert.That(child.Inherit<Token>(), Is.SameAs(tokenA));

        // move child to tree-B (different provider)
        var providerB = new TestNode("B");
        var tokenB    = new Token();
        providerB.Provide(tokenB);
        providerB.AdoptChild(child);

        Assert.That(child.Inherit<Token>(), Is.SameAs(tokenB));
    }

    [Test]
    public void Inherit_AfterReparentToNodeWithoutProvider_Throws()
    {
        var provider = new TestNode("provider");
        var middle   = new TestNode("middle");
        var child    = new TestNode("child");
        provider.AttachChild(child);
        provider.Provide(new Token());

        // move child under a node that provides nothing
        middle.AdoptChild(child);

        Assert.Throws<InvalidOperationException>(() => child.Inherit<Token>());
    }
}
