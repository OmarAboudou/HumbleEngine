using System.Reflection;

namespace HumbleEngine.Core;

public class HumbleTypeRegistry
{
    private Dictionary<Guid, Type> _types = [];

    public void Register(Type type)
    {
        HumbleTypeAttribute? attribute = type.GetCustomAttribute<HumbleTypeAttribute>();
        if (attribute == null)
        {
            throw new InvalidOperationException($"The type {type} has no HumbleType attribute.");
        }
        
        Guid id = Guid.Parse(attribute.Id);
        if(!_types.TryAdd(id, type))
        {
            throw new InvalidOperationException($"A type with id {id} has already been registered.");
        }
        
        Services.Logger.Debug<HumbleTypeChannel>($"Type {type} has been registered.");
    }

    public void Register(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes().Where(t => t.GetCustomAttribute<HumbleTypeAttribute>() != null))
        {
            Register(type);
        }
    }
    
    public Type Resolve(Guid id)
    {
        if (!_types.TryGetValue(id, out Type? type))
        {
            throw new ArgumentException($"No types registered for id {id}");
        }
        return type;
    }
}