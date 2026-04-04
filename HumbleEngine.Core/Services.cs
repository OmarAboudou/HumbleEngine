namespace HumbleEngine.Core;

/// <summary>
/// Provides shared, application-wide instances of core HumbleEngine services.
/// </summary>
public static class Services
{
    private static Dictionary<Type, object> _services = [];
    
    /// <summary>
    /// The shared <see cref="Logger"/> instance used throughout the engine.
    /// </summary>
    public static Logger Logger { get; } = new();
    
    public static HumbleTypeRegistry HumbleTypeRegistry { get; } = new();

    static Services()
    {
        AddService(Logger);
        AddService(HumbleTypeRegistry);
    }

    public static T? Get<T>() => GetAll<T>().FirstOrDefault();

    public static IEnumerable<T> GetAll<T>() => _services.Values.OfType<T>();

    private static void AddService<T>(T service)
    {
        if(service == null)
            throw new ArgumentNullException(nameof(service));

        if (_services.ContainsKey(typeof(T)))
        {
            throw new InvalidOperationException($"{typeof(T)} already registered");
        }
        
        _services[typeof(T)] = service;
    }

}