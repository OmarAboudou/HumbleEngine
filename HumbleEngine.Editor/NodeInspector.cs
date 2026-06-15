using System.Reflection;

namespace HumbleEngine;

/// <summary>
/// Discovers the inspectable properties of a <see cref="Node"/> by reflection:
/// every public property whose declared type implements <see cref="IObservableValue"/>
/// is exposed to the inspector. The discovery result is cached per concrete type
/// so the reflection cost is paid only once.
/// <para>
/// Convention: <b>public + IObservableValue = inspectable</b>. No attribute required.
/// A future <c>[HideInInspector]</c> attribute can opt out individual properties
/// when the need arises (al caso par cas, per [[feedback-api-visibility]]).
/// </para>
/// </summary>
public static class NodeInspector
{
    /// <summary>Pair of a property's display name and its observable value handle.</summary>
    /// <param name="Name">The C# property name, used as the inspector label.</param>
    /// <param name="Value">The live observable value read from the node.</param>
    public readonly record struct InspectableProperty(string Name, IObservableValue Value);

    private static readonly Dictionary<Type, PropertyInfo[]> _cache = [];

    /// <summary>
    /// Returns all inspectable properties of <paramref name="node"/> in declaration
    /// order. Each entry carries the property name and its live <see cref="IObservableValue"/>
    /// handle — the inspector can read <c>ValueType</c> to choose a widget and cast
    /// to <c>IObservableValue&lt;T&gt;</c> to read or write the value.
    /// </summary>
    public static IReadOnlyList<InspectableProperty> GetInspectableProperties(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var type  = node.GetType();
        var infos = GetOrBuildCache(type);

        var result = new List<InspectableProperty>(infos.Length);
        foreach (var info in infos)
        {
            if (info.GetValue(node) is IObservableValue value)
                result.Add(new InspectableProperty(info.Name, value));
        }
        return result;
    }

    /// <summary>
    /// Returns (and caches) the <see cref="PropertyInfo"/> array for <paramref name="type"/>:
    /// public instance properties whose declared type implements <see cref="IObservableValue"/>,
    /// in declaration order (base class first — <c>DeclaredOnly</c> per type, walked up).
    /// </summary>
    private static PropertyInfo[] GetOrBuildCache(Type type)
    {
        if (_cache.TryGetValue(type, out var cached))
            return cached;

        // Walk the hierarchy base-first so base-class properties appear first,
        // mirroring the visual top-to-bottom reading order of the inspector.
        var chain = new List<Type>();
        for (var t = type; t is not null && t != typeof(object); t = t.BaseType)
            chain.Add(t);
        chain.Reverse(); // base first

        var result = new List<PropertyInfo>();
        var seen   = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in chain)
        {
            foreach (var info in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                // Skip indexers, already-seen overrides, and non-IObservableValue types.
                if (info.GetIndexParameters().Length > 0) continue;
                if (!seen.Add(info.Name))               continue;
                if (!typeof(IObservableValue).IsAssignableFrom(info.PropertyType)) continue;

                result.Add(info);
            }
        }

        var array = result.ToArray();
        _cache[type] = array;
        return array;
    }
}
