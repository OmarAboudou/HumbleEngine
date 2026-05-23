using NUnit.Framework;
using System.Collections.Generic;

namespace HumbleEngine.Tests;

[TestFixture]
public class NodeLifecycleTests
{
    private sealed class TestNode : Node
    {
        private readonly List<string> _log;
        private readonly string _name;

        public TestNode(string name, List<string> log)
        {
            _name = name;
            _log  = log;
        }

        public override void OnTreeEntered()     => _log.Add($"{_name}.Entered");
        public override void OnChildrenEntered() => _log.Add($"{_name}.ChildrenEntered");
        public override void OnChildrenExited()  => _log.Add($"{_name}.ChildrenExited");
        public override void OnTreeExited()      => _log.Add($"{_name}.Exited");
    }

    [Test]
    public void EnterTree_SingleNode_FiresEnteredThenChildrenEntered()
    {
        var log  = new List<string>();
        var root = new TestNode("A", log);

        root.EnterTree();

        Assert.That(log, Is.EqualTo(new[] { "A.Entered", "A.ChildrenEntered" }));
    }

    [Test]
    public void EnterTree_PropagatesTopDown_ChildrenEnteredBottomUp()
    {
        //   A
        //  / \
        // B   C
        var log = new List<string>();
        var a   = new TestNode("A", log);
        var b   = new TestNode("B", log);
        var c   = new TestNode("C", log);
        a.Attach(b);
        a.Attach(c);

        a.EnterTree();

        Assert.That(log, Is.EqualTo(new[]
        {
            "A.Entered",
            "B.Entered", "B.ChildrenEntered",
            "C.Entered", "C.ChildrenEntered",
            "A.ChildrenEntered"
        }));
    }

    [Test]
    public void ExitTree_ChildrenExitedTopDown_ExitedBottomUp()
    {
        var log = new List<string>();
        var a   = new TestNode("A", log);
        var b   = new TestNode("B", log);
        var c   = new TestNode("C", log);
        a.Attach(b);
        a.Attach(c);
        a.EnterTree();
        log.Clear();

        a.ExitTree();

        Assert.That(log, Is.EqualTo(new[]
        {
            "A.ChildrenExited",
            "B.ChildrenExited", "B.Exited",
            "C.ChildrenExited", "C.Exited",
            "A.Exited"
        }));
    }

    [Test]
    public void SetParent_ToInTreeNode_FiresLifecycle()
    {
        var log  = new List<string>();
        var root = new TestNode("Root", log);
        var child = new TestNode("Child", log);
        root.EnterTree();
        log.Clear();

        root.Attach(child);

        Assert.That(log, Is.EqualTo(new[] { "Child.Entered", "Child.ChildrenEntered" }));
    }

    [Test]
    public void SetParent_ToNull_FromInTree_FiresExited()
    {
        var log   = new List<string>();
        var root  = new TestNode("Root", log);
        var child = new TestNode("Child", log);
        root.Attach(child);
        root.EnterTree();
        log.Clear();

        root.Detach(child);

        Assert.That(log, Is.EqualTo(new[] { "Child.ChildrenExited", "Child.Exited" }));
    }

    [Test]
    public void SetParent_OutsideTree_FiresNothing()
    {
        var log   = new List<string>();
        var root  = new TestNode("Root", log);
        var child = new TestNode("Child", log);

        root.Attach(child);

        Assert.That(log, Is.Empty);
    }

    [Test]
    public void Reparent_WithinTree_FiresExitThenEnter()
    {
        var log  = new List<string>();
        var root = new TestNode("Root", log);
        var a    = new TestNode("A", log);
        var b    = new TestNode("B", log);
        var child = new TestNode("Child", log);
        root.Attach(a);
        root.Attach(b);
        a.Attach(child);
        root.EnterTree();
        log.Clear();

        child.SetParent(b);

        Assert.That(log, Is.EqualTo(new[]
        {
            "Child.ChildrenExited", "Child.Exited",
            "Child.Entered",        "Child.ChildrenEntered"
        }));
    }

    [Test]
    public void IsInTree_ReflectsLifecycleState()
    {
        var log  = new List<string>();
        var root = new TestNode("Root", log);

        Assert.That(root._isInTree, Is.False);
        root.EnterTree();
        Assert.That(root._isInTree, Is.True);
        root.ExitTree();
        Assert.That(root._isInTree, Is.False);
    }
}
