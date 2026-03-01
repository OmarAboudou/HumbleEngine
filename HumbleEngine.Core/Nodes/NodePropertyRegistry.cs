using System.Collections.Concurrent;
using System.Reflection;

namespace HumbleEngine.Core;

/// <summary>
/// Représente un membre de node (propriété ou champ) décoré avec
/// [Exposed] et/ou [Overridable].
/// </summary>
public sealed class NodeProperty
{
    // L'accesseur est un détail d'implémentation — il n'a pas à fuiter
    // dans l'API publique. On le stocke dans un champ privé.
    private readonly IMemberAccessor _accessor;

    internal NodeProperty(
        string name,
        Type memberType,
        bool isExposed,
        bool isOverridable,
        IMemberAccessor accessor)
    {
        Name = name;
        MemberType = memberType;
        IsExposed = isExposed;
        IsOverridable = isOverridable;
        _accessor = accessor;
    }

    /// <summary>Nom du membre C# (ex: "DisplayName").</summary>
    public string Name { get; }

    /// <summary>Type de la valeur.</summary>
    public Type MemberType { get; }

    /// <summary>
    /// Indique si la valeur est visible dans l'inspecteur (membre lisible).
    /// Un membre peut être [Overridable] sans être [Exposed] — dans ce cas
    /// il est modifiable depuis une scène mais invisible dans l'inspecteur.
    /// </summary>
    public bool IsExposed { get; }

    /// <summary>
    /// Indique si la valeur peut être overridée depuis une EmbeddedScene.
    /// </summary>
    public bool IsOverridable { get; }

    /// <summary>
    /// Lit la valeur de ce membre sur un node donné.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Si le membre n'est pas [Exposed] (pas de getter accessible).
    /// </exception>
    public object? GetValue(Node node)
    {
        if (!IsExposed)
            throw new InvalidOperationException(
                $"Le membre '{Name}' n'est pas [Exposed] — sa valeur ne peut pas être lue " +
                "par le système d'inspection.");

        return _accessor.GetValue(node);
    }

    /// <summary>
    /// Écrit une valeur sur ce membre via réflexion.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Si le membre n'est pas [Overridable].
    /// </exception>
    public void SetValue(Node node, object? value)
    {
        if (!IsOverridable)
            throw new InvalidOperationException(
                $"Le membre '{Name}' n'est pas [Overridable] et ne peut pas être écrit " +
                "par le système de scènes.");

        _accessor.SetValue(node, value);
    }
}

/// <summary>
/// Abstraction interne qui unifie l'accès en lecture/écriture sur une
/// propriété C# ou un champ C#. Réflexion encapsulée ici, invisible
/// depuis l'extérieur du moteur.
/// </summary>
internal interface IMemberAccessor
{
    object? GetValue(Node node);
    void SetValue(Node node, object? value);
}

/// <summary>Accesseur pour une propriété C#.</summary>
internal sealed class PropertyAccessor : IMemberAccessor
{
    private readonly PropertyInfo _property;

    public PropertyAccessor(PropertyInfo property) => _property = property;

    public object? GetValue(Node node) => _property.GetValue(node);

    public void SetValue(Node node, object? value)
    {
        var setter = _property.GetSetMethod(nonPublic: true)
            ?? throw new InvalidOperationException(
                $"La propriété '{_property.Name}' est [Overridable] mais n'a pas de setter. " +
                "Ce cas devrait être détecté au chargement de la scène.");

        setter.Invoke(node, new[] { value });
    }
}

/// <summary>
/// Accesseur pour un champ C#. Les champs sont toujours accessibles
/// en écriture par réflexion, quel que soit leur modificateur d'accès.
/// </summary>
internal sealed class FieldAccessor : IMemberAccessor
{
    private readonly FieldInfo _field;

    public FieldAccessor(FieldInfo field) => _field = field;

    public object? GetValue(Node node) => _field.GetValue(node);

    public void SetValue(Node node, object? value) => _field.SetValue(node, value);
}

/// <summary>
/// Registre statique des membres [Exposed] et/ou [Overridable] par type de node.
///
/// La réflexion est coûteuse — ce registre garantit qu'on n'inspecte chaque type
/// qu'une seule fois, au premier accès, puis on met le résultat en cache.
/// Thread-safe grâce à ConcurrentDictionary.
/// </summary>
public static class NodePropertyRegistry
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<NodeProperty>> _cache = new();

    /// <summary>
    /// Retourne tous les membres [Exposed] et/ou [Overridable] déclarés
    /// sur le type du node donné et ses types parents.
    /// </summary>
    public static IReadOnlyList<NodeProperty> GetProperties(Node node)
        => GetProperties(node.GetType());

    /// <summary>
    /// Retourne tous les membres [Exposed] et/ou [Overridable] déclarés
    /// sur le type donné et ses types parents.
    /// </summary>
    public static IReadOnlyList<NodeProperty> GetProperties(Type nodeType)
        => _cache.GetOrAdd(nodeType, Inspect);

    /// <summary>
    /// Retourne uniquement les membres [Overridable] d'un type de node.
    /// Raccourci utile pour le système d'instanciation de scènes.
    /// </summary>
    public static IReadOnlyList<NodeProperty> GetOverridableProperties(Type nodeType)
        => GetProperties(nodeType).Where(p => p.IsOverridable).ToList();

    /// <summary>
    /// Retourne uniquement les membres [Exposed] d'un type de node,
    /// c'est-à-dire les membres dont la valeur est lisible dans l'inspecteur.
    /// </summary>
    public static IReadOnlyList<NodeProperty> GetExposedProperties(Type nodeType)
        => GetProperties(nodeType).Where(p => p.IsExposed).ToList();

    /// <summary>
    /// Retourne les erreurs de configuration détectées sur un type de node.
    /// Actuellement : propriétés [Overridable] sans setter.
    /// Appelé au chargement d'une scène pour produire les diagnostics SCN0014.
    /// </summary>
    public static IReadOnlyList<string> GetConfigurationErrors(Type nodeType)
    {
        var errors = new List<string>();

        var type = nodeType;
        while (type is not null && type != typeof(object))
        {
            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.DeclaredOnly;

            foreach (var prop in type.GetProperties(flags))
            {
                var isOverridable = prop.GetCustomAttribute<OverridableAttribute>() is not null;
                if (isOverridable && prop.GetSetMethod(nonPublic: true) is null)
                    errors.Add(
                        $"La propriété '{prop.Name}' sur '{type.Name}' est [Overridable] " +
                        "mais n'a pas de setter. Ajoutez un setter (public ou private).");
            }

            type = type.BaseType;
        }

        return errors;
    }

    // -------------------------------------------------------------------------
    // Inspection par réflexion — appelée une seule fois par type
    // -------------------------------------------------------------------------

    private static IReadOnlyList<NodeProperty> Inspect(Type nodeType)
    {
        var result = new List<NodeProperty>();

        // On parcourt manuellement la chaîne d'héritage, de la classe la plus
        // dérivée jusqu'à Node inclus. FlattenHierarchy ne remonte que les
        // membres statiques — pour les membres d'instance hérités, il faut
        // inspecter chaque type de la hiérarchie séparément.
        var type = nodeType;
        while (type is not null && type != typeof(object))
        {
            // DeclaredOnly limite l'inspection au type courant — sans ça,
            // on risque de retrouver les mêmes membres plusieurs fois en
            // remontant la chaîne.
            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.DeclaredOnly;

            foreach (var prop in type.GetProperties(flags))
            {
                // On évite les doublons si une propriété est déjà présente
                // (cas d'un override dans une classe dérivée).
                if (result.Any(p => p.Name == prop.Name))
                    continue;

                var isOverridable = prop.GetCustomAttribute<OverridableAttribute>() is not null;
                var isExposed = prop.GetCustomAttribute<ExposedAttribute>() is not null;

                if (!isOverridable && !isExposed) continue;
                if (isOverridable && prop.GetSetMethod(nonPublic: true) is null) continue;
                if (isExposed && prop.GetGetMethod(nonPublic: true) is null) continue;

                result.Add(new NodeProperty(
                    name: prop.Name,
                    memberType: prop.PropertyType,
                    isExposed: isExposed,
                    isOverridable: isOverridable,
                    accessor: new PropertyAccessor(prop)));
            }

            foreach (var field in type.GetFields(flags))
            {
                if (field.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)))
                    continue;
                if (result.Any(p => p.Name == field.Name))
                    continue;

                var isOverridable = field.GetCustomAttribute<OverridableAttribute>() is not null;
                var isExposed = field.GetCustomAttribute<ExposedAttribute>() is not null;

                if (!isOverridable && !isExposed) continue;

                result.Add(new NodeProperty(
                    name: field.Name,
                    memberType: field.FieldType,
                    isExposed: isExposed,
                    isOverridable: isOverridable,
                    accessor: new FieldAccessor(field)));
            }

            type = type.BaseType;
        }

        return result.AsReadOnly();
    }
}