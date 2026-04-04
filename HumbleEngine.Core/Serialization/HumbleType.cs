namespace HumbleEngine.Core;

public record HumbleType
{
    public Guid Id { get; }
    public Type RuntimeType { get; private set; }
    private readonly HumbleTypeRegistry _registry;

    public HumbleType(Guid id) : this(id, Services.HumbleTypeRegistry) { }
    public HumbleType(Guid id, HumbleTypeRegistry registry)
    {
        Id = id;
        _registry = registry;
    }

    public Type Resolve() => Resolve(Services.HumbleTypeRegistry);
    public Type Resolve(HumbleTypeRegistry registry)
    {
        RuntimeType = _registry.Resolve(Id);
        return RuntimeType;
    }
}