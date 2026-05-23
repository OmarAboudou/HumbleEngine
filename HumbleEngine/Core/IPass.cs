namespace HumbleEngine;

public interface IPass
{
    void Execute(PassContext context);
    bool ShouldExecute() => true;
}
