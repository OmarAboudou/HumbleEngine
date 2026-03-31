namespace HumbleEngine.Core;

public readonly record struct SignalParameterDefinition(Type Type, string Name)
{
    public static implicit operator SignalParameterDefinition((Type, string) fromTuple) => new(fromTuple.Item1, fromTuple.Item2);

    public override string ToString() => $"{Type.Name} {Name}";
}