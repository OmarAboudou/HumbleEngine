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
    /// If this node is attached to a <see cref="NodeTree"/>, the operation is deferred
    /// via an <see cref="AddChildCommand"/>. Otherwise, it is applied immediately.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if <paramref name="child"/> is this node itself.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="child"/> is already inside a tree, already has a parent,
    /// or is already among this node's children.
    /// </exception>
    public void AddChild(Node child)
    {
        ValidateAddChild(child);

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
    /// Attempts to add a node as a child of this node.
    /// </summary>
    /// <param name="child">The node to add as a child.</param>
    /// <returns><c>true</c> if the child was successfully added or queued; <c>false</c> otherwise.</returns>
    /// <remarks>
    /// If this node is attached to a <see cref="NodeTree"/>, the operation is deferred
    /// via an <see cref="AddChildCommand"/>. Otherwise, it is applied immediately.
    /// </remarks>
    public bool TryAddChild(Node child)
    {
        if (!IsAddChildValid(child)) return false;

        if (Tree == null)
        {
            AddChildRightAway(child);
        }
        else
        {
            Tree.QueueCommand(new AddChildCommand(this, child));
        }

        return true;
    }

    private void ValidateAddChild(Node child)
    {
        if (child == this)
            throw new ArgumentException($"The node {this} cannot add itself as a child.", nameof(child));
        if (child.Tree != null)
            throw new InvalidOperationException($"The child node {child} is still inside a tree.");
        if (child.Parent != null)
            throw new InvalidOperationException($"The node {child} cannot be added as a child of the node {this}, because it already has a parent.");
        if (Children.Contains(child))
            throw new InvalidOperationException($"The node {child} is already a child of the node {this}.");
    }

    private bool IsAddChildValid(Node child)
    {
        return child != this
               && child.Tree == null
               && child.Parent == null
               && !Children.Contains(child);
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
    /// If this node is attached to a <see cref="NodeTree"/>, the operation is deferred
    /// via a <see cref="RemoveChildCommand"/>. Otherwise, it is applied immediately.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if <paramref name="child"/> is this node itself.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="child"/> has no parent, its parent is not this node,
    /// or it is not among this node's children.
    /// </exception>
    public void RemoveChild(Node child)
    {
        ValidateRemoveChild(child);

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
    /// Attempts to remove a child node from this node.
    /// </summary>
    /// <param name="child">The child node to remove.</param>
    /// <returns><c>true</c> if the child was successfully removed or queued for removal; <c>false</c> otherwise.</returns>
    /// <remarks>
    /// If this node is attached to a <see cref="NodeTree"/>, the operation is deferred
    /// via a <see cref="RemoveChildCommand"/>. Otherwise, it is applied immediately.
    /// </remarks>
    public bool TryRemoveChild(Node child)
    {
        if (!IsRemoveChildValid(child)) return false;

        if (Tree == null)
        {
            RemoveChildRightAway(child);
        }
        else
        {
            Tree.QueueCommand(new RemoveChildCommand(this, child));
        }

        return true;
    }

    private void ValidateRemoveChild(Node child)
    {
        if (child == this)
            throw new ArgumentException($"The node {this} cannot remove itself from its children.", nameof(child));
        if (child.Parent == null)
            throw new InvalidOperationException($"The node {child} does not have a parent.");
        if (child.Parent != this)
            throw new InvalidOperationException($"The node {child} does not have the node {this} as a parent.");
        if (!Children.Contains(child))
            throw new InvalidOperationException($"The node {child} is not among the children of the node {this}.");
    }

    private bool IsRemoveChildValid(Node child)
    {
        return child != this
               && child.Parent == this
               && Children.Contains(child);
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