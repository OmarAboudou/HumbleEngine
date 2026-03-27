namespace HumbleEngine.Core;

public record AddChildCommand(Node Parent, Node Child) : NodeTreeCommand
{
    public void Execute(NodeTree tree)
    {
        // TODO : Implement
        throw new NotImplementedException();
    }
}