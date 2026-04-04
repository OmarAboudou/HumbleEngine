namespace HumbleEngine.Core;

[AttributeUsage(AttributeTargets.Class,  AllowMultiple = false, Inherited = false)]
public class HumbleTypeAttribute(string id) : Attribute
{
    public string Id { get; }= id;
}