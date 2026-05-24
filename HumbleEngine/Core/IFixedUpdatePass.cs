namespace HumbleEngine;

public interface IFixedUpdatePass : IPass
{
    void Execute(Node root, double delta, BlackBoard board);
}
