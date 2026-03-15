namespace HumbleEngine.Core;

public class Node
{
    #region NodeHierarchy

    public Node? Parent { get; private set; }
    public List<Node> Children { get; private set; } = [];
    
    public void AddChild(Node child)
    {
        if (!Children.Contains(child))
        {
            Children.Add(child);
        }    
    }

    public void RemoveChild(Node child)
    {
        if (Children.Contains(child))
        {
            Children.Remove(child);
        }
    }
    
    #endregion
    
    #region LifeCycle
    public virtual void TreeEntered() { }
    public virtual void Process() { }
    public virtual void TreeExiting() { }
    #endregion
    
}