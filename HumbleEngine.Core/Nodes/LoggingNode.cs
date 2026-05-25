namespace HumbleEngine.Core;

public class LoggingNode : Node, IUpdated
{
    public void OnUpdate(double delta)
    {
        Console.WriteLine($"{this} : processing (delta = {delta} | {1/delta} fps )");
    }
}