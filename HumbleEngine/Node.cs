using System.Collections;

namespace HumbleEngine;

public abstract class Node : IEnumerable<Node>
{
    internal readonly Property<Node?> _parent = new();
    private readonly ListProperty<Node> _children = new();
    
    public ReadOnlyProperty<Node?> Parent => _parent.AsReadOnly();
    public IReadOnlyList<Node> Children => _children.AsReadOnly();

    public virtual void InitializeProperties()
    {
        _parent.ValueChanged.Connect((oldP, newP) =>
        {
            oldP?._children.Remove(this);
            newP?._children.Add(this);
        });
    }
    
    public void SetParent(Node? parent)
    {
        if(!SetParentCheck(parent))
            return;
        
        SetParentUnsafe(parent);
    }
    private bool SetParentCheck(Node? parent)
    {
        if (parent == this)
        {
            Console.WriteLine($"A {nameof(Node)} cannot be its own child.");
            return false;
        }
        if (ReferenceEquals(_parent.Value, parent))
        {
            Console.WriteLine($"{this} is already a child of {parent}");
            return false;
        }
        
        return true;
    }
    private void SetParentUnsafe(Node? parent) => this._parent.Value = parent;

    public void Attach(Node node) => node.SetParent(this);
    private bool AttachCheck(Node node) => node.SetParentCheck(this);
    private void AttachUnsafe(Node node) => node.SetParentUnsafe(this);

    public void Detach(Node node) => node.SetParent(null);
    private bool DetachCheck(Node node) => node.SetParentCheck(null);
    private void DetachUnsafe(Node node) => node.SetParentUnsafe(null);
    
    public IEnumerator<Node> GetEnumerator() 
        => _children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() 
        => GetEnumerator();
}