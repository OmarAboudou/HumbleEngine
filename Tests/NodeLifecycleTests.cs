using HumbleEngine.Core;
using NUnit.Framework;

namespace HumbleEngine.Core.Tests;

// Node de test qui enregistre l'ordre d'invocation de chaque callback.
// On utilise une liste partagée passée au constructeur pour pouvoir
// observer l'ordre global entre plusieurs nodes.
file sealed class LifecycleNode : Node
{
    private readonly List<string> _log;
    private readonly string _name;

    public LifecycleNode(string name, List<string> log)
    {
        _name = name;
        _log = log;
    }

    protected override void OnTreeEntering()   => _log.Add($"{_name}.OnTreeEntering");
    protected override void OnTreeEntered()    => _log.Add($"{_name}.OnTreeEntered");
    protected override void OnReady()          => _log.Add($"{_name}.OnReady");
    protected override void OnProcess(float delta)        => _log.Add($"{_name}.OnProcess");
    protected override void OnPhysicsProcess(float delta) => _log.Add($"{_name}.OnPhysicsProcess");
    protected override void OnTreeExiting()    => _log.Add($"{_name}.OnTreeExiting");
    protected override void OnTreeExited()     => _log.Add($"{_name}.OnTreeExited");
}

[TestFixture]
public class NodeLifecycleTests
{
    // -------------------------------------------------------------------------
    // Ordre des callbacks sur un node seul
    // -------------------------------------------------------------------------

    [Test]
    public void SingleNode_ReceivesCallbacks_InCorrectOrder_OnSetRoot()
    {
        var log = new List<string>();
        var tree = new NodeTree();
        var root = new LifecycleNode("root", log);

        tree.SetRoot(root);

        // On attend exactement cet ordre pour un node seul.
        Assert.That(log, Is.EqualTo(new[]
        {
            "root.OnTreeEntering",
            "root.OnTreeEntered",
            "root.OnReady"
        }));
    }

    [Test]
    public void SingleNode_ReceivesExitCallbacks_InCorrectOrder_OnRemove()
    {
        var log = new List<string>();
        var tree = new NodeTree();
        var root = new LifecycleNode("root", log);
        var child = new LifecycleNode("child", log);
        root.AddChild(child);
        tree.SetRoot(root);
        log.Clear(); // On réinitialise pour n'observer que la sortie

        root.RemoveChild(child);
        tree.FlushPendingChanges();

        Assert.That(log, Is.EqualTo(new[]
        {
            "child.OnTreeExiting",
            "child.OnTreeExited"
        }));
    }

    // -------------------------------------------------------------------------
    // Ordre de propagation sur une hiérarchie parent → enfant
    // -------------------------------------------------------------------------

    [Test]
    public void OnTreeEntering_PropagatesParentToChildren()
    {
        // OnTreeEntering doit descendre : parent d'abord, puis enfants.
        // C'est l'onde "aller" qui prépare chaque node à entrer.
        var log = new List<string>();
        var tree = new NodeTree();
        var root = new LifecycleNode("root", log);
        var child = new LifecycleNode("child", log);
        var grandchild = new LifecycleNode("grandchild", log);
        root.AddChild(child);
        child.AddChild(grandchild);

        tree.SetRoot(root);

        // OnTreeEntering doit apparaître dans l'ordre parent → enfant → petit-enfant.
        var enteringEvents = log.Where(e => e.EndsWith(".OnTreeEntering")).ToList();
        Assert.That(enteringEvents, Is.EqualTo(new[]
        {
            "root.OnTreeEntering",
            "child.OnTreeEntering",
            "grandchild.OnTreeEntering"
        }));
    }

    [Test]
    public void OnTreeEntered_PropagatesChildrenToParent()
    {
        // OnTreeEntered remonte : enfants d'abord, puis parent.
        // Garantie : quand le parent reçoit OnTreeEntered, tous ses enfants
        // ont déjà reçu le leur — le parent peut les interroger en sécurité.
        var log = new List<string>();
        var tree = new NodeTree();
        var root = new LifecycleNode("root", log);
        var child = new LifecycleNode("child", log);
        var grandchild = new LifecycleNode("grandchild", log);
        root.AddChild(child);
        child.AddChild(grandchild);

        tree.SetRoot(root);

        var enteredEvents = log.Where(e => e.EndsWith(".OnTreeEntered")).ToList();
        Assert.That(enteredEvents, Is.EqualTo(new[]
        {
            "grandchild.OnTreeEntered",
            "child.OnTreeEntered",
            "root.OnTreeEntered"
        }));
    }

    [Test]
    public void OnReady_PropagatesChildrenToParent()
    {
        // OnReady remonte également : quand le parent reçoit OnReady,
        // tous ses enfants sont déjà prêts. C'est la garantie fondamentale
        // qui rend OnReady utile pour les initialisations complexes.
        var log = new List<string>();
        var tree = new NodeTree();
        var root = new LifecycleNode("root", log);
        var child = new LifecycleNode("child", log);
        var grandchild = new LifecycleNode("grandchild", log);
        root.AddChild(child);
        child.AddChild(grandchild);

        tree.SetRoot(root);

        var readyEvents = log.Where(e => e.EndsWith(".OnReady")).ToList();
        Assert.That(readyEvents, Is.EqualTo(new[]
        {
            "grandchild.OnReady",
            "child.OnReady",
            "root.OnReady"
        }));
    }

    [Test]
    public void OnTreeExiting_PropagatesChildrenToParent()
    {
        // OnTreeExiting remonte : les enfants sont notifiés avant le parent.
        // Tree est encore accessible à ce stade — les nodes peuvent
        // encore interagir avec l'arbre pour se nettoyer proprement.
        var log = new List<string>();
        var tree = new NodeTree();
        var root = new LifecycleNode("root", log);
        var child = new LifecycleNode("child", log);
        var grandchild = new LifecycleNode("grandchild", log);
        root.AddChild(child);
        child.AddChild(grandchild);
        tree.SetRoot(root);
        log.Clear();

        root.RemoveChild(child);
        tree.FlushPendingChanges();

        var exitingEvents = log.Where(e => e.EndsWith(".OnTreeExiting")).ToList();
        Assert.That(exitingEvents, Is.EqualTo(new[]
        {
            "grandchild.OnTreeExiting",
            "child.OnTreeExiting"
        }));
    }

    [Test]
    public void OnTreeExiting_HasAccessToTree()
    {
        // Garantie critique : Tree est encore non-null pendant OnTreeExiting.
        // Sans cette garantie, les nodes ne pourraient pas se désenregistrer
        // proprement des services globaux de l'arbre (ex: signaux, physics).
        var tree = new NodeTree();
        var root = new CheckingNode();
        var child = new TreeCapturingNode();
        root.AddChild(child);
        tree.SetRoot(root);

        NodeTree? capturedTree = null;
        child.OnExitingCallback = () => capturedTree = child.Tree;

        root.RemoveChild(child);
        tree.FlushPendingChanges();

        Assert.That(capturedTree, Is.EqualTo(tree));
    }

    [Test]
    public void OnTreeExited_TreeIsNull()
    {
        // Garantie symétrique : Tree est null pendant OnTreeExited.
        var tree = new NodeTree();
        var root = new CheckingNode();
        var child = new TreeCapturingNode();
        root.AddChild(child);
        tree.SetRoot(root);

        NodeTree? capturedTree = new NodeTree(); // valeur sentinelle non-null
        child.OnExitedCallback = () => capturedTree = child.Tree;

        root.RemoveChild(child);
        tree.FlushPendingChanges();

        Assert.That(capturedTree, Is.Null);
    }

    // -------------------------------------------------------------------------
    // OnReady — invoqué une seule fois
    // -------------------------------------------------------------------------

    [Test]
    public void OnReady_IsInvokedOnlyOnce_EvenAfterReattachment()
    {
        // OnReady ne doit jamais être rappelé si le node est retiré
        // puis réattaché à un arbre. C'est une garantie d'initialisation unique.
        var log = new List<string>();
        var tree = new NodeTree();
        var root = new LifecycleNode("root", log);
        var child = new LifecycleNode("child", log);
        root.AddChild(child);
        tree.SetRoot(root);

        // Premier attachement : OnReady est appelé.
        var readyCount = log.Count(e => e == "child.OnReady");
        Assert.That(readyCount, Is.EqualTo(1));

        // On retire l'enfant puis on le réattache.
        root.RemoveChild(child);
        tree.FlushPendingChanges();
        root.AddChild(child);
        tree.FlushPendingChanges();

        // OnReady ne doit pas être rappelé.
        readyCount = log.Count(e => e == "child.OnReady");
        Assert.That(readyCount, Is.EqualTo(1));
    }

    // -------------------------------------------------------------------------
    // Process et PhysicsProcess
    // -------------------------------------------------------------------------

    [Test]
    public void Process_FlushesChanges_ThenPropagates()
    {
        var log = new List<string>();
        var tree = new NodeTree();
        var root = new LifecycleNode("root", log);
        tree.SetRoot(root);
        log.Clear();

        tree.Process(0.016f);

        Assert.That(log, Contains.Item("root.OnProcess"));
    }

    [Test]
    public void PhysicsProcess_FlushesChanges_ThenPropagates()
    {
        var log = new List<string>();
        var tree = new NodeTree();
        var root = new LifecycleNode("root", log);
        tree.SetRoot(root);
        log.Clear();

        tree.PhysicsProcess(0.016f);

        Assert.That(log, Contains.Item("root.OnPhysicsProcess"));
    }
}

// Nodes utilitaires pour les tests qui nécessitent des callbacks personnalisés.
file sealed class CheckingNode : Node
{
    public Action? OnExitingCallback { get; set; }
    protected override void OnTreeExiting() => OnExitingCallback?.Invoke();
}

file sealed class TreeCapturingNode : Node
{
    public Action? OnExitingCallback { get; set; }
    public Action? OnExitedCallback { get; set; }
    protected override void OnTreeExiting() => OnExitingCallback?.Invoke();
    protected override void OnTreeExited() => OnExitedCallback?.Invoke();
}