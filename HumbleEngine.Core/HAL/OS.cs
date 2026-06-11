namespace HumbleEngine;

public abstract class OS
{
    private static OS? _current;

    public static OS Current => _current
        ?? throw new InvalidOperationException(
            "OS non initialisé. Référencez un assembly de plateforme (ex: HumbleEngine.Linux).");

    public static void Register(OS os)
    {
        if (_current is not null)
            throw new InvalidOperationException("Un OS est déjà enregistré.");
        _current = os;
    }

    public abstract string Name { get; }
    public abstract IReadOnlyList<IGraphicsBackend> AvailableGraphicsBackends { get; }
    public abstract IGraphicsBackend DefaultGraphicsBackend { get; }

    public IGraphicsBackend GetGraphicsBackend(string name) =>
        AvailableGraphicsBackends.FirstOrDefault(b => b.Name == name)
        ?? throw new KeyNotFoundException($"Backend graphique '{name}' introuvable.");
}
