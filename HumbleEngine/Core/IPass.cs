namespace HumbleEngine;

public interface IPass
{
    void Execute(Node root, double delta);
    bool ShouldExecute() => true;
}
