namespace HumbleEngine;

[AttributeUsage(AttributeTargets.Class)]
public sealed class FixedUpdatePassAttribute(string? name = null, int order = 0) : Attribute
{
    public string? Name  { get; } = name;
    public int     Order { get; } = order;
}
