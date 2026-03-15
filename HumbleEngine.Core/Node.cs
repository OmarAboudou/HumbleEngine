namespace HumbleEngine.Core;

public class Node
{
    #region NodeHierarchy
    
    internal NodeTree? NodeTree; 
    public Node? Parent { get; private set; }
    
    private List<Node> _children = [];
    public IReadOnlyList<Node> Children => this.GetChildren();
    public IReadOnlyList<Node> GetChildren() => this._children; 
    
    public void AddChild(Node child)
    {
        if (!this._children.Contains(child))
        {
            child.SetNodeTreeRecursively(this.NodeTree);
            this._children.Add(child);
        }    
    }

    public void RemoveChild(Node child)
    {
        if (this._children.Contains(child))
        {
            this._children.Remove(child);
            child.SetNodeTreeRecursively(null);
        }
    }

    internal void SetNodeTreeRecursively(NodeTree? tree)
    {
        this.NodeTree = tree;
        Stack<Node> nodeStack = new();
        nodeStack.Push(this);

        while (nodeStack.Count > 0)
        {
            Node current = nodeStack.Pop();
            foreach (Node child in current.GetChildren())
            {
                nodeStack.Push(child);
            }
            current.NodeTree = tree;
        }
        
    }
    
    #endregion
    
    #region LifeCycle
    public virtual void TreeEntered() { }
    public virtual void Process() { }
    public virtual void TreeExiting() { }
    #endregion
    
}