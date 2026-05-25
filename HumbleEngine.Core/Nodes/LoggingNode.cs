namespace HumbleEngine.Core;

public class LoggingNode : Node, IUpdated, IFixedUpdated
{
    public void OnUpdate(double delta)
    {
        Console.WriteLine($"{this} : Update (delta = {delta} | {1/delta} fps )");
    }

    public void OnFixedUpdate(double delta)
    {
        Console.WriteLine($"{this} : FixedUpdate (delta = {delta} | {1/delta} fps )");
        
    }
}