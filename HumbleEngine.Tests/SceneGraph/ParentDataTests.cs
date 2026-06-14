namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for the parent-data mechanism on <see cref="Node"/>: a parent that
/// overrides <see cref="Node.CreateParentData"/> stamps a fresh instance on each
/// child at adoption, and the framework clears it on departure (remove, re-parent,
/// dispose). The data's lifecycle rides the single adoption choke point — the
/// <see cref="Node.Parent"/> setter.
/// </summary>
public sealed class ParentDataTests
{
    private sealed class Stamp : ParentData
    {
        public int Tag { get; init; }
    }

    /// <summary>A parent that stamps a <see cref="Stamp"/> carrying its own tag.</summary>
    private sealed class Stamper : Node
    {
        public int Tag { get; init; }
        protected override ParentData? CreateParentData() => new Stamp { Tag = Tag };
        public void Add(Node child)    => Attach(child);
        public void Remove(Node child) => Detach(child);
        public void Move(Node child)   => Adopt(child);
    }

    /// <summary>A parent that defines no parent-data (the default).</summary>
    private sealed class Plain : Node
    {
        public void Add(Node child)  => Attach(child);
        public void Move(Node child) => Adopt(child);
    }

    [Test]
    public void Adoption_StampsTheParentData()
    {
        var parent = new Stamper { Tag = 7 };
        var child  = new Plain();

        parent.Add(child);

        Assert.That(child.ParentData, Is.InstanceOf<Stamp>());
        Assert.That(((Stamp)child.ParentData!).Tag, Is.EqualTo(7));
    }

    [Test]
    public void ParentWithoutFactory_LeavesItNull()
    {
        var parent = new Plain();
        var child  = new Plain();

        parent.Add(child);

        Assert.That(child.ParentData, Is.Null);
    }

    [Test]
    public void Removal_ClearsTheParentData()
    {
        var parent = new Stamper();
        var child  = new Plain();
        parent.Add(child);

        parent.Remove(child);

        Assert.That(child.ParentData, Is.Null);
    }

    [Test]
    public void Reparenting_RestampsWithTheNewParentsData()
    {
        var a     = new Stamper { Tag = 1 };
        var b     = new Stamper { Tag = 2 };
        var child = new Plain();
        a.Add(child);

        b.Move(child);   // Adopt: leaves a, joins b

        Assert.That(((Stamp)child.ParentData!).Tag, Is.EqualTo(2));
    }

    [Test]
    public void ReparentingToAPlainParent_ClearsIt()
    {
        var a     = new Stamper { Tag = 1 };
        var plain = new Plain();
        var child = new Plain();
        a.Add(child);

        plain.Move(child);

        Assert.That(child.ParentData, Is.Null);
    }

    [Test]
    public void DisposingAStampedChild_ClearsIt()
    {
        var parent = new Stamper();
        var child  = new Plain();
        parent.Add(child);

        child.Dispose();

        Assert.That(child.ParentData, Is.Null);
    }
}
