using HumbleEngine.Core;
using NUnit.Framework;

namespace HumbleEngine.Core.Tests;

// -------------------------------------------------------------------------
// Nodes de test
// -------------------------------------------------------------------------

internal sealed class ContainerNode : Node
{
    // Node cible interne — c'est là que les enfants injectés atterrissent.
    public readonly InnerNode Inner = new();

    [Slot(Description = "Entrées du conteneur.")]
    public NodeSlot<EntryNode> Entries => GetSlot<EntryNode>(Inner);
}

// Node cible interne du ContainerNode.
internal sealed class InnerNode : Node { }

// Type injectable dans le slot Entries.
internal sealed class EntryNode : Node { }

// Type incompatible — ne peut pas être injecté dans Entries.
internal sealed class OtherNode : Node { }

// -------------------------------------------------------------------------
// Tests
// -------------------------------------------------------------------------

[TestFixture]
internal sealed class NodeSlotTests
{
    private NodeTree _tree = null!;
    private ContainerNode _container = null!;

    [SetUp]
    public void SetUp()
    {
        // On construit un arbre minimal : le ContainerNode contient
        // son Inner comme enfant direct, ce qui est nécessaire pour
        // que les opérations différées fonctionnent correctement.
        _tree = new NodeTree();
        _container = new ContainerNode();
        _container.AddChild(_container.Inner);
        _tree.SetRoot(_container);
    }

    // -------------------------------------------------------------------------
    // Cache — GetSlot retourne toujours la même instance
    // -------------------------------------------------------------------------

    [Test]
    public void GetSlot_ReturnsSameInstance_OnMultipleCalls()
    {
        // La propriété Entries est calculée (=> GetSlot<T>(...)) — sans cache,
        // chaque accès créerait une nouvelle instance. On vérifie que le cache
        // garantit la cohérence de référence.
        var first = _container.Entries;
        var second = _container.Entries;

        Assert.That(ReferenceEquals(first, second), Is.True);
    }

    // -------------------------------------------------------------------------
    // Redirection vers le node cible
    // -------------------------------------------------------------------------

    [Test]
    public void Slot_Target_IsTheInnerNode()
    {
        Assert.That(_container.Entries.Target, Is.EqualTo(_container.Inner));
    }

    [Test]
    public void Add_InjectsNode_AsChildOfTarget()
    {
        var entry = new EntryNode();

        _container.Entries.Add(entry);
        _tree.FlushPendingChanges();

        // L'entrée doit être un enfant du node cible interne, pas du ContainerNode.
        Assert.That(_container.Inner.Children, Contains.Item(entry));
        Assert.That(_container.Children, Does.Not.Contain(entry));
    }

    [Test]
    public void Add_WithIndex_InjectsAtCorrectPosition()
    {
        var entry1 = new EntryNode();
        var entry2 = new EntryNode();
        var entry3 = new EntryNode();

        _container.Entries.Add(entry1);
        _container.Entries.Add(entry3);
        _tree.FlushPendingChanges();

        _container.Entries.Add(entry2, index: 1);
        _tree.FlushPendingChanges();

        Assert.That(_container.Inner.Children,
            Is.EqualTo(new Node[] { entry1, entry2, entry3 }));
    }

    [Test]
    public void Remove_DetachesNode_FromTarget()
    {
        var entry = new EntryNode();
        _container.Entries.Add(entry);
        _tree.FlushPendingChanges();

        _container.Entries.Remove(entry);
        _tree.FlushPendingChanges();

        Assert.That(_container.Inner.Children, Does.Not.Contain(entry));
        Assert.That(entry.Parent, Is.Null);
    }

    // -------------------------------------------------------------------------
    // Items et HasItems
    // -------------------------------------------------------------------------

    [Test]
    public void Items_ReturnsInjectedNodes()
    {
        var entry1 = new EntryNode();
        var entry2 = new EntryNode();
        _container.Entries.Add(entry1);
        _container.Entries.Add(entry2);
        _tree.FlushPendingChanges();

        Assert.That(_container.Entries.Items, Is.EqualTo(new[] { entry1, entry2 }));
    }

    [Test]
    public void HasItems_IsFalse_WhenSlotIsEmpty()
    {
        Assert.That(_container.Entries.HasItems, Is.False);
    }

    [Test]
    public void HasItems_IsTrue_AfterInjection()
    {
        _container.Entries.Add(new EntryNode());
        _tree.FlushPendingChanges();

        Assert.That(_container.Entries.HasItems, Is.True);
    }

    // -------------------------------------------------------------------------
    // Contrainte de type
    // -------------------------------------------------------------------------

    [Test]
    public void Slot_DoesNotExposeIncompatibleNodes_InItems()
    {
        // Si par un moyen externe un OtherNode se retrouvait comme enfant
        // de Inner, Items ne doit pas le retourner — le filtre OfType<T>
        // garantit la contrainte de type.
        var other = new OtherNode();
        _container.Inner.AddChild(other);
        _tree.FlushPendingChanges();

        Assert.That(_container.Entries.Items, Is.Empty);
    }
}