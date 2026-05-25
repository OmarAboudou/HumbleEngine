namespace HumbleEngine.Core;

public class LoggingNode : Node, IProcessable
{
    public void Process(double delta)
    {
        Console.WriteLine($"{this} : processing (delta = {delta} | {1/delta} fps )");
    }
}