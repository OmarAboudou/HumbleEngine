namespace HumbleEngine;

/// <summary>
/// Platform descriptor — entry point for accessing available backends.
/// Registered automatically via <c>[ModuleInitializer]</c> in the platform assembly.
/// </summary>
public abstract class OS
{
    private static OS? _current;

    /// <summary>
    /// The current platform instance, set by the platform assembly on load.
    /// </summary>
    /// <exception cref="InvalidOperationException">No platform assembly has been loaded.</exception>
    public static OS Current => _current
        ?? throw new InvalidOperationException(
            "OS non initialisé. Référencez un assembly de plateforme (ex: HumbleEngine.Linux).");

    /// <summary>
    /// Registers the platform instance. Called exactly once by the platform assembly's
    /// <c>[ModuleInitializer]</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">An OS instance is already registered.</exception>
    public static void Register(OS os)
    {
        if (_current is not null)
            throw new InvalidOperationException("Un OS est déjà enregistré.");
        _current = os;
    }

    /// <summary>Human-readable platform name, e.g. "Linux" or "Windows".</summary>
    public abstract string Name { get; }

    /// <summary>All graphics backends available on this platform.</summary>
    public abstract IReadOnlyList<IGraphicsBackend> AvailableGraphicsBackends { get; }

    /// <summary>The default graphics backend for this platform.</summary>
    public abstract IGraphicsBackend DefaultGraphicsBackend { get; }

    /// <summary>Returns the graphics backend with the given name.</summary>
    /// <exception cref="KeyNotFoundException">No backend with that name exists.</exception>
    public IGraphicsBackend GetGraphicsBackend(string name) =>
        AvailableGraphicsBackends.FirstOrDefault(b => b.Name == name)
        ?? throw new KeyNotFoundException($"Backend graphique '{name}' introuvable.");
}
