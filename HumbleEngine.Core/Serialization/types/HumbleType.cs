namespace HumbleEngine.Core;

public readonly record struct HumbleType(Guid Id)
{
    public Type Resolve() => Services.HumbleTypeRegistry.Resolve(Id);

}