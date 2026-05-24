namespace HumbleEngine;

public interface IUpdatePass : IPass
{
    void Execute(Node root, double delta, BlackBoard board);
}
