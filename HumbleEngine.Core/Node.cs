namespace HumbleEngine.Core;

/// <summary>
/// Base building block of the node tree. Every element in the game world is a <see cref="Node"/>
/// or a subclass of it. Nodes form a tree hierarchy through parent-child relationships.
/// </summary>
/// <remarks>
/// A node can exist in two states: detached (not part of any <see cref="NodeTree"/>)
/// or attached (registered in an <see cref="NodeTree"/>).
/// When attached, structural changes (adding/removing children) are deferred via commands
/// and executed during <see cref="NodeTree.FlushCommands"/>.
/// When detached, structural changes are applied immediately.
/// </remarks>
public class Node
{
    /// <summary>
    /// The parent of this node in the tree hierarchy, or <c>null</c> if this node is a root or detached.
    /// </summary>
    public Node? Parent { get; private set; }

    /// <summary>
    /// The <see cref="NodeTree"/> this node belongs to, or <c>null</c> if the node is not part of any tree.
    /// </summary>
    public NodeTree? Tree { get; internal set; }

    /// <summary>
    /// A read-only view of this node's children, in insertion order.
    /// </summary>
    public IReadOnlyList<Node> Children => _children;
    private List<Node> _children = [];

    #region Tree Structure

    #region Adding a child

    /// <summary>
    /// Adds a node as a child of this node.
    /// </summary>
    /// <param name="child">The node to add as a child.</param>
    /// <remarks>
    /// If this node is attached to an <see cref="NodeTree"/>, the operation is deferred
    /// via an <see cref="AddChildCommand"/>. Otherwise, it is applied immediately.
    /// The operation is silently ignored if <see cref="CanAddChild"/> returns <c>false</c>.
    /// </remarks>
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

    /// <summary>
    /// Checks whether <paramref name="child"/> can be added as a child of this node.
    /// </summary>
    /// <param name="child">The candidate child node.</param>
    /// <returns><c>true</c> if the child can be added; <c>false</c> otherwise.</returns>
    /// <remarks>
    /// Returns <c>false</c> and logs to <see cref="Console.Error"/> if:
    /// <list type="bullet">
    ///   <item><description><paramref name="child"/> is already inside a tree.</description></item>
    ///   <item><description><paramref name="child"/> is this node itself.</description></item>
    ///   <item><description><paramref name="child"/> already has a parent.</description></item>
    ///   <item><description><paramref name="child"/> is already among this node's children.</description></item>
    /// </list>
    /// </remarks>
    public bool CanAddChild(Node child)
    {
        if (child.Tree != null)
        {
            Console.Error.WriteLine($"The child node {child} is still inside a tree");
            return false;
        }

        if (child == this)
        {
            Console.Error.WriteLine($"the node {this} cannot add itself as a child.");
            return false;
        }

        if (child.Parent != null)
        {
            Console.Error.WriteLine($"The node {child} cannot be added as a child of the node {this}, because it already has a parent.");
            return false;
        }

        if (Children.Contains(child))
        {
            Console.Error.WriteLine($"The node {child} is already a child of the node {this}.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Immediately attaches <paramref name="child"/> to this node, bypassing the command queue.
    /// </summary>
    /// <param name="child">The node to attach.</param>
    internal void AddChildRightAway(Node child)
    {
        child.Parent = this;
        this._children.Add(child);
    }    

    #endregion

    #region Removing a child

    /// <summary>
    /// Removes a child node from this node.
    /// </summary>
    /// <param name="child">The child node to remove.</param>
    /// <remarks>
    /// If this node is attached to an <see cref="NodeTree"/>, the operation is deferred
    /// via a <see cref="RemoveChildCommand"/>. Otherwise, it is applied immediately.
    /// The operation is silently ignored if <see cref="CanRemoveChild"/> returns <c>false</c>.
    /// </remarks>
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

    /// <summary>
    /// Checks whether <paramref name="child"/> can be removed from this node's children.
    /// </summary>
    /// <param name="child">The candidate child node to remove.</param>
    /// <returns><c>true</c> if the child can be removed; <c>false</c> otherwise.</returns>
    /// <remarks>
    /// Returns <c>false</c> and logs to <see cref="Console.Error"/> if:
    /// <list type="bullet">
    ///   <item><description><paramref name="child"/> is this node itself.</description></item>
    ///   <item><description><paramref name="child"/> has no parent.</description></item>
    ///   <item><description><paramref name="child"/>'s parent is not this node.</description></item>
    ///   <item><description><paramref name="child"/> is not among this node's children.</description></item>
    /// </list>
    /// </remarks>
    public bool CanRemoveChild(Node child)
    {
        if (child == this)
        {
            Console.Error.WriteLine($"the node {this} cannot remove itself from its children.");
            return false;
        }

        if (child.Parent == null)
        {
            Console.Error.WriteLine($"The node {child} does not have a parent.");
            return false;
        }

        if (child.Parent != this)
        {
            Console.Error.WriteLine($"The node {child} does not have the node {this} as a parent.");
            return false;
        }

        if (!Children.Contains(child))
        {
            Console.Error.WriteLine($"The node {child} is not among the children of the node {this}.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Immediately detaches <paramref name="child"/> from this node, bypassing the command queue.
    /// </summary>
    /// <param name="child">The node to detach.</param>
    internal void RemoveChildRightAway(Node child)
    {
        this._children.Remove(child);
        child.Parent = null;
    }    

    #endregion

    #endregion
    
    #region Life Cycle

    /// <summary>
    /// Called every frame during the game loop. Override to implement per-frame logic.
    /// </summary>
    /// <param name="delta">The elapsed time in seconds since the previous frame.</param>
    public virtual void Process(double delta){}

    /// <summary>
    /// Called every physics tick. Override to implement physics-related logic.
    /// </summary>
    /// <param name="delta">The elapsed time in seconds since the previous physics tick.</param>
    public virtual void PhysicsProcess(double delta){}

    /// <summary>
    /// Called when this node enters an <see cref="NodeTree"/>. Override to run initialization
    /// logic that depends on being inside a tree.
    /// </summary>
    public virtual void TreeEntered(){}

    /// <summary>
    /// Called when this node is about to leave its <see cref="NodeTree"/>. Override to run
    /// cleanup logic before the node is detached from the tree.
    /// </summary>
    public virtual void TreeExiting(){}
    
    #endregion

    #region Utils

    /// <summary>
    /// Enumerates all nodes in this node's subtree (including itself) in depth-first prefix order.
    /// </summary>
    /// <returns>An enumerable of nodes, starting with this node, then its descendants depth-first left-to-right.</returns>
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