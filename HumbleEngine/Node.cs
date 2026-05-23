namespace HumbleEngine;

public abstract class Node
{
    private readonly Property<Node?> _parent = new();
    public ReadOnlyProperty<Node?> Parent => _parent.AsReadOnly();
    
    
}