namespace HumbleEngine.Core;

public class Node
{
    #region NodeHierarchy

    public Node? Parent { get; private set; }
    
    private List<Node> _children = [];
    public IReadOnlyList<Node> Children => this.GetChildren();
    public IReadOnlyList<Node> GetChildren() => this._children; 
    
    public void AddChild(Node child)
    {
        if (!this._children.Contains(child))
        {
            this._children.Add(child);
        }    
    }

    public void RemoveChild(Node child)
    {
        if (this._children.Contains(child))
        {
            this._children.Remove(child);
        }
    }
    
    #endregion
    
    #region LifeCycle
    public virtual void TreeEntered() { }
    public virtual void Process() { }
    public virtual void TreeExiting() { }
    #endregion
    
}