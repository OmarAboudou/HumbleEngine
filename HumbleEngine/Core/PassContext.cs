namespace HumbleEngine;

public readonly struct PassContext
{
    public Node   Root  { get; }
    public double Delta { get; }

    public PassContext(Node root, double delta)
    {
        Root  = root;
        Delta = delta;
    }
}
