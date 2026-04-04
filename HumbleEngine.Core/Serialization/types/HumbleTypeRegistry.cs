using System.Reflection;

namespace HumbleEngine.Core;

/// <summary>
/// Registre central des types sérialisables du moteur.
/// Associe chaque <see cref="Guid"/> déclaré via <see cref="HumbleTypeAttribute"/> au <see cref="System.Type"/> C# correspondant.
/// </summary>
public class HumbleTypeRegistry
{
    private Dictionary<Guid, Type> _types = [];

    /// <summary>
    /// Enregistre un type dans le registre.
    /// Le type doit être décoré de <see cref="HumbleTypeAttribute"/>.
    /// </summary>
    /// <param name="type">Le type à enregistrer.</param>
    /// <exception cref="InvalidOperationException">
    /// Levée si le type n'a pas de <see cref="HumbleTypeAttribute"/>,
    /// ou si un type avec le même identifiant est déjà enregistré.
    /// </exception>
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

    /// <summary>
    /// Enregistre tous les types d'une assembly qui sont décorés de <see cref="HumbleTypeAttribute"/>.
    /// </summary>
    /// <param name="assembly">L'assembly à scanner.</param>
    public void Register(Assembly assembly)
    {
        Type[] types = assembly.GetTypes().Where(t => t.GetCustomAttribute<HumbleTypeAttribute>() != null).ToArray();
        foreach (Type type in types)
        {
            Register(type);
        }
        Services.Logger.Debug<HumbleTypeChannel>($"{types.Length} type(s) registered from assembly {assembly.GetName().Name}.");
    }
    
    /// <summary>
    /// Retourne le <see cref="System.Type"/> associé à l'identifiant donné.
    /// </summary>
    /// <param name="id">L'identifiant du type à résoudre.</param>
    /// <returns>Le <see cref="System.Type"/> correspondant.</returns>
    /// <exception cref="ArgumentException">Levée si aucun type n'est enregistré pour cet identifiant.</exception>
    public Type Resolve(Guid id)
    {
        if (!_types.TryGetValue(id, out Type? type))
        {
            throw new ArgumentException($"No types registered for id {id}");
        }
        return type;
    }
}