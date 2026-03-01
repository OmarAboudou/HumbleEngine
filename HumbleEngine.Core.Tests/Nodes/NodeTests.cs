using HumbleEngine.Core;
using NUnit.Framework;

namespace HumbleEngine.Core.Tests;

// Node concret minimal pour pouvoir instancier Node (abstraite) dans les tests.
// Le modificateur 'file' limite sa visibilité à ce fichier uniquement.
internal sealed class TestNode : Node
{
    public TestNode() { }
    public TestNode(Guid id) : base(id) { }
}

[TestFixture]
public class NodeTests
{
    // -------------------------------------------------------------------------
    // Identité
    // -------------------------------------------------------------------------

    [Test]
    public void Node_HasUniqueGuid_WhenCreatedWithoutExplicitId()
    {
        var a = new TestNode();
        var b = new TestNode();
        Assert.That(a.Id, Is.Not.EqualTo(b.Id));
    }

    [Test]
    public void Node_UsesProvidedGuid_WhenCreatedWithExplicitId()
    {
        var id = Guid.NewGuid();
        var node = new TestNode(id);
        Assert.That(node.Id, Is.EqualTo(id));
    }

    // -------------------------------------------------------------------------
    // Structure hors NodeTree — modifications immédiates
    // -------------------------------------------------------------------------

    [Test]
    public void AddChild_SetsParent_WhenOutsideTree()
    {
        var parent = new TestNode();
        var child = new TestNode();

        parent.AddChild(child);

        Assert.That(child.Parent, Is.EqualTo(parent));
    }

    [Test]
    public void AddChild_AppendsToChildren_WhenOutsideTree()
    {
        var parent = new TestNode();
        var child1 = new TestNode();
        var child2 = new TestNode();

        parent.AddChild(child1);
        parent.AddChild(child2);

        Assert.That(parent.Children, Is.EqualTo(new[] { child1, child2 }));
    }

    [Test]
    public void AddChild_InsertsAtIndex_WhenIndexProvided()
    {
        var parent = new TestNode();
        var child1 = new TestNode();
        var child2 = new TestNode();
        var child3 = new TestNode();

        parent.AddChild(child1);
        parent.AddChild(child3);
        parent.AddChild(child2, index: 1); // insère entre child1 et child3

        Assert.That(parent.Children, Is.EqualTo(new[] { child1, child2, child3 }));
    }

    [Test]
    public void RemoveChild_ClearsParent_WhenOutsideTree()
    {
        var parent = new TestNode();
        var child = new TestNode();
        parent.AddChild(child);

        parent.RemoveChild(child);

        Assert.That(child.Parent, Is.Null);
        Assert.That(parent.Children, Is.Empty);
    }

    [Test]
    public void ChildrenOrder_IsPreserved()
    {
        var parent = new TestNode();
        var children = Enumerable.Range(0, 5).Select(_ => new TestNode()).ToList();
        foreach (var child in children)
            parent.AddChild(child);

        Assert.That(parent.Children, Is.EqualTo(children));
    }

    // -------------------------------------------------------------------------
    // Invariants — violations attendues
    // -------------------------------------------------------------------------

    [Test]
    public void AddChild_Throws_WhenChildAlreadyHasParent()
    {
        var parent1 = new TestNode();
        var parent2 = new TestNode();
        var child = new TestNode();
        parent1.AddChild(child);

        // On ne peut pas ajouter un node qui a déjà un parent
        // sans l'en retirer d'abord — peu importe le tree.
        Assert.That(() => parent2.AddChild(child), Throws.InvalidOperationException);
    }

    [Test]
    public void AddChild_Throws_WhenChildIsInDifferentTree_WithoutExplicitRemoval()
    {
        var tree1 = new NodeTree();
        var tree2 = new NodeTree();
        var root1 = new TestNode();
        var root2 = new TestNode();
        var child = new TestNode();

        tree1.SetRoot(root1);
        tree2.SetRoot(root2);
        root1.AddChild(child);
        tree1.FlushPendingChanges();

        // Le transfert implicite (sans RemoveChild préalable) est interdit
        Assert.That(() => root2.AddChild(child), Throws.InvalidOperationException);
    }

    [Test]
    public void Node_CanBeTransferred_BetweenTrees_AfterExplicitRemoval()
    {
        var tree1 = new NodeTree();
        var tree2 = new NodeTree();
        var root1 = new TestNode();
        var root2 = new TestNode();
        var child = new TestNode();

        tree1.SetRoot(root1);
        tree2.SetRoot(root2);

        // Attache dans tree1
        root1.AddChild(child);
        tree1.FlushPendingChanges();
        Assert.That(child.Tree, Is.EqualTo(tree1));

        // Retire de tree1 explicitement
        root1.RemoveChild(child);
        tree1.FlushPendingChanges();
        Assert.That(child.Tree, Is.Null);

        // Attache dans tree2 — le transfert est maintenant valide
        root2.AddChild(child);
        tree2.FlushPendingChanges();
        Assert.That(child.Tree, Is.EqualTo(tree2));
        Assert.That(child.Parent, Is.EqualTo(root2));
    }

    [Test]
    public void AddChild_Throws_WhenCycleDetected()
    {
        var a = new TestNode();
        var b = new TestNode();
        a.AddChild(b);

        // Tenter d'ajouter 'a' comme enfant de 'b' créerait un cycle
        Assert.That(() => b.AddChild(a), Throws.InvalidOperationException);
    }

    [Test]
    public void RemoveChild_Throws_WhenNodeIsNotDirectChild()
    {
        var parent = new TestNode();
        var other = new TestNode();

        Assert.That(() => parent.RemoveChild(other), Throws.InvalidOperationException);
    }

    // -------------------------------------------------------------------------
    // Référence Tree
    // -------------------------------------------------------------------------

    [Test]
    public void Tree_IsNull_WhenNodeIsDetached()
    {
        var node = new TestNode();

        Assert.That(node.Tree, Is.Null);
        Assert.That(node.IsInsideTree, Is.False);
    }
}

[TestFixture]
public class NodeTreeTests
{
    // -------------------------------------------------------------------------
    // Injection de Tree
    // -------------------------------------------------------------------------

    [Test]
    public void SetRoot_InjectsTree_OnRootNode()
    {
        var tree = new NodeTree();
        var root = new TestNode();

        tree.SetRoot(root);

        Assert.That(root.Tree, Is.EqualTo(tree));
        Assert.That(root.IsInsideTree, Is.True);
    }

    [Test]
    public void SetRoot_InjectsTree_OnAllDescendants()
    {
        var tree = new NodeTree();
        var root = new TestNode();
        var child = new TestNode();
        var grandchild = new TestNode();

        // On construit la hiérarchie avant de l'attacher au tree
        root.AddChild(child);
        child.AddChild(grandchild);
        tree.SetRoot(root);

        Assert.That(child.Tree, Is.EqualTo(tree));
        Assert.That(grandchild.Tree, Is.EqualTo(tree));
    }

    [Test]
    public void SetRoot_Throws_WhenRootAlreadySet()
    {
        var tree = new NodeTree();
        tree.SetRoot(new TestNode());

        Assert.That(() => tree.SetRoot(new TestNode()), Throws.InvalidOperationException);
    }

    // -------------------------------------------------------------------------
    // Opérations différées — AddChild dans le tree
    // -------------------------------------------------------------------------

    [Test]
    public void AddChild_IsDeferredInsideTree_UntilFlush()
    {
        var tree = new NodeTree();
        var root = new TestNode();
        tree.SetRoot(root);

        var child = new TestNode();
        root.AddChild(child); // différé

        // Avant le flush, l'enfant n'est pas encore attaché
        Assert.That(root.Children, Is.Empty);
        Assert.That(child.Parent, Is.Null);

        tree.FlushPendingChanges();

        // Après le flush, tout est en place
        Assert.That(root.Children, Has.Count.EqualTo(1));
        Assert.That(child.Parent, Is.EqualTo(root));
        Assert.That(child.Tree, Is.EqualTo(tree));
    }

    [Test]
    public void RemoveChild_IsDeferredInsideTree_UntilFlush()
    {
        var tree = new NodeTree();
        var root = new TestNode();
        var child = new TestNode();

        // On construit la hiérarchie avant d'attacher au tree
        root.AddChild(child);
        tree.SetRoot(root);

        root.RemoveChild(child); // différé

        // Avant le flush : child est encore là
        Assert.That(root.Children, Has.Count.EqualTo(1));

        tree.FlushPendingChanges();

        // Après le flush : retiré proprement
        Assert.That(root.Children, Is.Empty);
        Assert.That(child.Parent, Is.Null);
        Assert.That(child.Tree, Is.Null);
    }

    [Test]
    public void FlushPendingChanges_AppliesOperationsInOrder()
    {
        var tree = new NodeTree();
        var root = new TestNode();
        tree.SetRoot(root);

        var child1 = new TestNode();
        var child2 = new TestNode();

        root.AddChild(child1);
        root.AddChild(child2);
        tree.FlushPendingChanges();

        // L'ordre d'insertion doit être préservé après le flush
        Assert.That(root.Children, Is.EqualTo(new[] { child1, child2 }));
    }

    [Test]
    public void AddChild_InjectsTree_AfterFlush()
    {
        var tree = new NodeTree();
        var root = new TestNode();
        tree.SetRoot(root);

        var child = new TestNode();
        root.AddChild(child);
        tree.FlushPendingChanges();

        Assert.That(child.Tree, Is.EqualTo(tree));
    }

    [Test]
    public void RemoveChild_WithdrawsTree_AfterFlush()
    {
        var tree = new NodeTree();
        var root = new TestNode();
        var child = new TestNode();
        root.AddChild(child);
        tree.SetRoot(root);

        root.RemoveChild(child);
        tree.FlushPendingChanges();

        Assert.That(child.Tree, Is.Null);
        Assert.That(child.IsInsideTree, Is.False);
    }
}