using System.Collections;

namespace HumbleEngine;

public abstract class Node : IEnumerable<Node>
{
    public Node()
    {
        _parent.ValueChanged.Connect((oldP, newP) =>
        {
            oldP?._children.Remove(this);
            newP?._children.Add(this);

            bool wasInTree = oldP?._isInTree ?? false;
            bool nowInTree = newP?._isInTree ?? false;

            if      (!wasInTree &&  nowInTree) EnterTree();
            else if ( wasInTree && !nowInTree) ExitTree();
            else if ( wasInTree &&  nowInTree) { ExitTree(); EnterTree(); }
        });
    }

    #region Parent/Child relationship

    internal readonly Property<Node?> _parent = new();
    private readonly ListProperty<Node> _children = new();
    public ReadOnlyProperty<Node?> Parent => _parent.AsReadOnly();
    public IReadOnlyList<Node> Children => _children.AsReadOnly();

    public void SetParent(Node? parent)
    {
        if (parent == this)
            throw new ArgumentException($"A {nameof(Node)} cannot be its own child.", nameof(parent));
        if (ReferenceEquals(_parent.Value, parent))
            return;
        _parent.Value = parent;
    }

    public void Attach(Node node) => node.SetParent(this);
    public void Detach(Node node) => node.SetParent(null);

    #endregion

    #region Lifecycle

    internal bool _isInTree = false;

    internal void EnterTree()
    {
        _isInTree = true;
        OnTreeEntered();
        foreach (var child in _children)
            child.EnterTree();
        OnChildrenEntered();
    }

    internal void ExitTree()
    {
        OnChildrenExited();
        foreach (var child in _children)
            child.ExitTree();
        OnTreeExited();
        _isInTree = false;
    }

    public virtual void OnTreeEntered()     { }
    public virtual void OnChildrenEntered() { }
    public virtual void OnChildrenExited()  { }
    public virtual void OnTreeExited()      { }

    #endregion

    #region Traversal

    public IEnumerable<Node> GetSubtreeDepthFirst()
    {
        Stack<Node> stack = new();
        stack.Push(this);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;
            foreach (var child in node._children.Reverse())
                stack.Push(child);
        }
    }

    public IEnumerable<Node> GetSubtreeReverseDepthFirst() => GetSubtreeDepthFirst().Reverse();

    #endregion
    
    public IEnumerator<Node> GetEnumerator() 
        => _children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() 
        => GetEnumerator();
}