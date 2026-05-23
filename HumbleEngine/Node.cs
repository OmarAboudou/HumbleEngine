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
        {
            throw new ArgumentException($"A {nameof(Node)} cannot be its own child.", nameof(parent));
        }
        if (ReferenceEquals(_parent.Value, parent))
        {
            Console.WriteLine($"{this} is already a child of {parent}");
            return;
        }
        
        _parent.Value = parent;
    }

    public void Attach(Node node) => node.SetParent(this);
    public void Detach(Node node) => node.SetParent(null);
    #endregion

    #region Life Cycle

    public virtual void OnTreeEntered() { }
    public virtual void OnChildrenEntered() { }
    public virtual void OnChildrenExited() { }
    public virtual void OnTreeExited() { }
    
    #endregion
    

    
    public IEnumerator<Node> GetEnumerator() 
        => _children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() 
        => GetEnumerator();
}