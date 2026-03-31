namespace HumbleEngine.Core;

/// <summary>
/// Describes a single parameter of a signal, holding its runtime type and display name.
/// </summary>
public readonly record struct SignalParameterDefinition
{
    /// <summary>The runtime type of the parameter.</summary>
    public readonly Type Type;
    /// <summary>The display name of the parameter, used in logging and tooling.</summary>
    public readonly string Name;

    internal SignalParameterDefinition(Type type, string name)
    {
        Type = type;
        Name = name;
    }
    public static implicit operator SignalParameterDefinition((Type, string) fromTuple) => new(fromTuple.Item1, fromTuple.Item2);

    public override string ToString() => $"{Type.Name} {Name}";
}