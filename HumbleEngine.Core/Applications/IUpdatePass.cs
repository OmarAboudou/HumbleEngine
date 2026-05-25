namespace HumbleEngine.Core;

public interface IUpdatePass
{
    public void Execute(Node root, double delta);
}