namespace HumbleEngine;

public interface IFixedUpdatePass
{
    public void Execute(Node root, double delta);
}