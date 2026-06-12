namespace HumbleEngine;

/// <summary>
/// Platform descriptor — entry point for accessing available backends.
/// The application entry point is responsible for calling <see cref="Register"/>
/// with the appropriate platform instance before accessing <see cref="Current"/>.
/// </summary>
public abstract class OS
{
    private static OS? _current;

    /// <summary>
    /// The current platform instance. Available after the application entry point
    /// has called <see cref="Register"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Register"/> has not been called.</exception>
    public static OS Current => _current
        ?? throw new InvalidOperationException(
            "No OS registered. Call OS.Register(new LinuxOS()) at application startup.");

    /// <summary>
    /// Registers the platform instance. Call this once at application startup,
    /// before accessing <see cref="Current"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">An OS instance is already registered.</exception>
    public static void Register(OS os)
    {
        if (_current is not null)
            throw new InvalidOperationException("An OS instance is already registered.");
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
        ?? throw new KeyNotFoundException($"No graphics backend named '{name}'.");
}
