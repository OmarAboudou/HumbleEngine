namespace HumbleEngine.Core;

public class Node
{
    public Node? Parent { get; private set; }
    public NodeTree? Tree { get; internal set; }
    public IReadOnlyList<Node> Children => _children;
    private List<Node> _children = [];

    #region Tree Structure

    #region Adding a child

    public void AddChild(Node child)
    {
        if(!CanAddChild(child)) return;

        if (Tree == null)
        {
            AddChildRightAway(child);
        }
        else
        {
            Tree.QueueCommand(new AddChildCommand(this, child));
        }
    }
    public bool CanAddChild(Node child)
    {
        if (child == this)
        {
            Console.WriteLine($"the node {this} cannot add itself as a child.");
            return false;
        }

        if (child.Parent != null)
        {
            Console.WriteLine($"The node {child} cannot be added as a child of the node {this}, because it already has a parent.");
            return false;
        }

        if (Children.Contains(child))
        {
            Console.WriteLine($"The node {child} is already a child of the node {this}.");
            return false;
        }
        return true;
    }
    internal void AddChildRightAway(Node child)
    {
        child.Parent = this;
        this._children.Add(child);
    }    

    #endregion

    #region Removing a child

    public void RemoveChild(Node child)
    {
        if(!CanRemoveChild(child)) return;

        if (Tree == null)
        {
            RemoveChildRightAway(child);
        }
        else
        {
            Tree.QueueCommand(new RemoveChildCommand(this, child));
        }
        
    }
    public bool CanRemoveChild(Node child)
    {
        if (child == this)
        {
            Console.WriteLine($"the node {this} cannot remove itself from its children.");
            return false;
        }

        if (child.Parent == null)
        {
            Console.WriteLine($"The node {child} does not have a parent.");
            return false;
        }
        
        if (child.Parent != this)
        {
            Console.WriteLine($"The node {child} does not have the node {this} as a parent.");
            return false;
        }
        
        if (!Children.Contains(child))
        {
            Console.WriteLine($"The node {child} is not among the children of the node {this}.");
            return false;
        }
        
        return true;
    }
    internal void RemoveChildRightAway(Node child)
    {
        this._children.Remove(child);
        child.Parent = null;
    }    

    #endregion

    #endregion
    
    #region Life Cycle

    public virtual void Process(double delta){}
    public virtual void PhysicsProcess(double delta){}
    
    public virtual void TreeEntered(){}
    public virtual void TreeExiting(){}
    
    #endregion

    #region Utils

    public IEnumerable<Node> GetSubtreeInPrefixOrder()
    {
        Stack<Node> nodeStack = new();
        nodeStack.Push(this);
        
        while (nodeStack.Count > 0)
        {
            Node current = nodeStack.Pop();
            yield return current;
            
            for (int i = current.Children.Count - 1; i >= 0; i--)
            {
                nodeStack.Push(current.Children[i]);
            }
        }
    }

    #endregion
    
}