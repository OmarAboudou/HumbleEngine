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

    /// <summary>
    /// The name of this node. Used for identification in logs and debug tools.
    /// Defaults to the class name of the node (e.g. <c>"Player"</c> for a <c>Player</c> node).
    /// </summary>
    private string _name;

    /// <inheritdoc cref="_name"/>>
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            this.Emit(OnRenamed, value);
        }
    }

    public Signal<string> OnRenamed { get; }

    /// <summary>
    /// Creates a new node. <see cref="Name"/> is initialized to the node's class name.
    /// </summary>
    public Node()
    {
        Name = GetType().Name;
        OnRenamed = this.CreateSignal<string>(nameof(OnRenamed), "name");
        OnChildAdded = this.CreateSignal<Node>(nameof(OnChildAdded), "child");
        OnChildRemoved = this.CreateSignal<Node>(nameof(OnChildRemoved), "child");
        OnTreeEntered = this.CreateSignal(nameof(OnTreeEntered));
        OnReady = this.CreateSignal(nameof(OnReady));
        OnUnready = this.CreateSignal(nameof(OnUnready));
        OnTreeExiting = this.CreateSignal(nameof(OnTreeExiting));
    }

    #region Tree Structure

    #region Adding a child
    public Signal<Node> OnChildAdded { get; }

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
        _children.Add(child);
        this.Emit(OnChildAdded, child);
    }    

    #endregion

    #region Removing a child

    public Signal<Node> OnChildRemoved { get; }
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
        _children.Remove(child);
        child.Parent = null;
        this.Emit(OnChildRemoved, child);
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

    #region Entering
    
    public Signal OnTreeEntered { get; }
    
    /// <summary>
    /// Called when this node enters a <see cref="NodeTree"/>, before its children have entered.
    /// Override to run initialization logic that depends on being inside a tree but does not
    /// require children to be ready yet.
    /// </summary>
    /// <remarks>
    /// At the time this is called, children may not yet have entered the tree.
    /// Use <see cref="OnReady"/> instead if your initialization depends on children being ready.
    /// </remarks>
    public virtual void TreeEntered(){}

    #endregion

    #region Readying

    public Signal OnReady { get; }

    /// <summary>
    /// Called after this node and all of its children have entered the tree and received
    /// <see cref="TreeEntered"/>. Override to run initialization logic that requires
    /// the entire subtree to be ready.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="TreeEntered"/>, <see cref="OnReady"/> is guaranteed to be called
    /// after all children in the subtree have already received their own <see cref="OnReady"/>.
    /// </remarks>
    
    public virtual void Ready(){}

    #endregion

    #region Unreadying

    public Signal OnUnready { get; }
    
    /// <summary>
    /// Called when this node is about to leave its <see cref="NodeTree"/>, before its children
    /// have been unreadied. Override to run cleanup logic that must happen before children are
    /// invalidated.
    /// </summary>
    /// <remarks>
    /// <see cref="Unready"/> is the symmetric counterpart of <see cref="OnReady"/>.
    /// Unlike <see cref="TreeExiting"/>, which is called after children have exited,
    /// <see cref="Unready"/> is called before children receive their own <see cref="Unready"/>.
    /// </remarks>
    public virtual void Unready(){}

    #endregion

    #region Exiting
    
    public Signal OnTreeExiting { get; }
    
    /// <summary>
    /// Called when this node is about to leave its <see cref="NodeTree"/>, after all of its
    /// children have already received <see cref="TreeExiting"/>. Override to run cleanup logic
    /// once the entire subtree has exited.
    /// </summary>
    /// <remarks>
    /// At the time this is called, children have already received <see cref="TreeExiting"/>
    /// and are about to be detached from the tree.
    /// Use <see cref="Unready"/> instead if your cleanup must happen before children are invalidated.
    /// </remarks>
    public virtual void TreeExiting(){}
    
    #endregion
    
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
    
    /// <summary>
    /// Enumerates all nodes in this node's subtree (including itself) in the exact reverse of
    /// depth-first prefix order. This guarantees that every child is yielded before its parent.
    /// </summary>
    /// <returns>An enumerable of nodes in reverse prefix order.</returns>
    public IEnumerable<Node> GetSubtreeInReversePrefixOrder()                                             
    {               
        Stack<Node> nodeStack = new();
        Stack<Node> result = new();                                                                 
        nodeStack.Push(this);
                                                                                                  
        while (nodeStack.Count > 0)
        {
            Node current = nodeStack.Pop();
            result.Push(current);                                                                   
   
            foreach (Node child in current.Children.Reverse())                                         
                nodeStack.Push(child);
        }

        while (result.Count > 0)                                                                    
            yield return result.Pop();
    }  

    public override string ToString() => Name;
    
    #endregion

}