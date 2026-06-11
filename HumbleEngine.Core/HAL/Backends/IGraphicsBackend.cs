namespace HumbleEngine;

public interface IGraphicsBackend : IDisposable
{
    string Name { get; }
    IReadOnlyList<Type> CompatibleWindowBackends { get; }

    // Implémentation par défaut : vérifie si le type du backend est dans la liste.
    // Overridable pour des vérifications runtime supplémentaires (ex: extensions Vulkan).
    bool Supports(IWindowBackend backend) =>
        CompatibleWindowBackends.Contains(backend.GetType());

    void Initialize();
    IRenderer CreateRenderer(IGraphicsSurface surface);
}
