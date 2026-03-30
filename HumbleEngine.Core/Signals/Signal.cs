namespace HumbleEngine.Core;

public class Signal
{
    public readonly string Name;
    public readonly object Owner;
    
    public Signal(object owner, string name)
    {
        Owner = owner;
        Name = name;
    }

    public override string ToString() => $"{Owner}:{Name}";
}