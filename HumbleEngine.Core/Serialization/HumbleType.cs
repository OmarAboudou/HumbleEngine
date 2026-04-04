namespace HumbleEngine.Core;

public record HumbleType
{
    public Guid Id { get; }
    
    private Type? _runtimeType;
    public Type RuntimeType
    {
        get
        {
            _runtimeType ??= Resolve();
            if(_runtimeType == null) 
                throw new InvalidOperationException($"Could not resolve type for id {Id}");
            
            return _runtimeType;
        }
    }

    private readonly HumbleTypeRegistry _registry;

    public HumbleType(Guid id) : this(id, Services.HumbleTypeRegistry) { }
    public HumbleType(Guid id, HumbleTypeRegistry registry)
    {
        Id = id;
        _registry = registry;
    }

    public Type Resolve() => Resolve(Services.HumbleTypeRegistry);
    public Type Resolve(HumbleTypeRegistry registry) => registry.Resolve(Id);
}