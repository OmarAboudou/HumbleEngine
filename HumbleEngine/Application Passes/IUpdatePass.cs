namespace HumbleEngine;

public interface IUpdatePass
{
    public void Execute(Node root, double delta);
}