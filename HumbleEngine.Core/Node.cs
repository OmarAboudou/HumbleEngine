namespace HumbleEngine.Core;

public class Node
{
    public Node? Parent { get; private set; }
    public NodeTree? Tree { get; internal set; }
    public IReadOnlyList<Node> Children => _children;
    private List<Node> _children = [];

    public void AddChild(Node node)
    {
        bool canAdd = CanAddChild(node);        
        if(!canAdd) return;
        
        node.Parent = this;
        this._children.Add(node);
    }
    public bool CanAddChild(Node node)
    {
        if (node == this)
        {
            Console.WriteLine($"the node {this} cannot add itself as a child.");
            return false;
        }

        if (node.Parent != null)
        {
            Console.WriteLine($"The node {node} cannot be added as a child of the node {this}, because it already has a parent.");
            return false;
        }

        if (Children.Contains(node))
        {
            Console.WriteLine($"The node {node} is already a child of the node {this}.");
            return false;
        }
        return true;
    }
    

    public void RemoveChild(Node node)
    {
        bool canRemove = CanRemoveChild(node);
        if (!canRemove) return;
        
        this._children.Remove(node);
        node.Parent = null;
    }
    public bool CanRemoveChild(Node node)
    {
        if (node == this)
        {
            Console.WriteLine($"the node {this} cannot remove itself from its children.");
            return false;
        }

        if (node.Parent == null)
        {
            Console.WriteLine($"The node {node} does not have a parent.");
            return false;
        }
        
        if (node.Parent != this)
        {
            Console.WriteLine($"The node {node} does not have the node {this} as a parent.");
            return false;
        }
        
        if (!Children.Contains(node))
        {
            Console.WriteLine($"The node {node} is not among the children of the node {this}.");
            return false;
        }
        
        return true;
    }
    
    public virtual void Process(double delta){}
    public virtual void PhysicsProcess(double delta){}
    
}