using Silk.NET.Maths;
using Silk.NET.Windowing;
using SkiaSharp;

namespace HumbleEngine;

public sealed class Application : IDisposable
{
    private readonly IWindow _window;
    private GRContext?  _grContext;  // contexte GPU SkiaSharp — wraps le contexte OpenGL
    private SKSurface?  _surface;   // surface de rendu liée au framebuffer de la fenêtre

    public Node? Root { get; set; }

    public Application(string title = "HumbleEngine", int width = 800, int height = 600)
    {
        var options = WindowOptions.Default;
        // WindowOptions.Default crée une fenêtre OpenGL 3.3 core profile
        options.Title  = title;
        options.Size   = new Vector2D<int>(width, height);
        // SkiaSharp utilise le stencil buffer pour le clipping — 8 bits requis
        options.PreferredStencilBufferBits = 8;

        _window = Window.Create(options);
        // Les delegates sont enregistrés ici mais appelés plus tard par Silk.NET
        _window.Load    += OnLoad;
        _window.Update  += OnUpdate;
        _window.Render  += OnRender;
        _window.Resize  += OnResize;
        _window.Closing += OnClosing;
    }

    // Démarre la boucle événementielle — bloque jusqu'à la fermeture de la fenêtre
    public void Run() => _window.Run();

    private void OnLoad()
    {
        // À ce stade, Silk.NET a créé la fenêtre et rendu le contexte OpenGL courant.
        // GRGlInterface.Create() sans argument détecte automatiquement le contexte GL
        // courant — plus fiable que passer un lambda de proc addresses.
        var glInterface = GRGlInterface.Create();

        if (glInterface is null)
            throw new InvalidOperationException(
                "SkiaSharp n'a pas pu créer l'interface OpenGL. " +
                "Vérifiez que OpenGL 3.3+ est disponible.");

        // GRContext est le pont entre SkiaSharp et le GPU.
        // Il cache les shaders, les textures et gère les ressources GPU.
        _grContext = GRContext.CreateGl(glInterface);

        if (_grContext is null)
            throw new InvalidOperationException(
                "SkiaSharp n'a pas pu créer le contexte GPU (GRContext).");

        CreateSurface();

        Root?.Init();
        Root?.MarkLayoutDirty();
    }

    private void CreateSurface()
    {
        _surface?.Dispose();

        var (w, h) = (_window.FramebufferSize.X, _window.FramebufferSize.Y);

        // GRGlFramebufferInfo décrit le framebuffer OpenGL cible.
        // 0 = FBO par défaut (le framebuffer de la fenêtre).
        // 0x8058 = GL_RGBA8, le format de pixel du framebuffer.
        var fbInfo = new GRGlFramebufferInfo(fboId: 0, format: 0x8058);

        // GRBackendRenderTarget représente la surface GPU à laquelle SkiaSharp va écrire.
        // sampleCount: 0 = pas de multisampling, stencilBits: 8 = pour le clipping.
        var renderTarget = new GRBackendRenderTarget(w, h, sampleCount: 0, stencilBits: 8, fbInfo);

        // SKSurface est la surface de dessin. GRSurfaceOrigin.BottomLeft car OpenGL
        // a l'origine en bas à gauche, contrairement à SkiaSharp (haut à gauche) —
        // cette option compense le flip vertical automatiquement.
        _surface = SKSurface.Create(_grContext, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);

        if (_surface is null)
            throw new InvalidOperationException(
                "SkiaSharp n'a pas pu créer la surface de rendu (SKSurface).");
    }

    private void OnUpdate(double delta) => Root?.Update((float)delta);

    private void OnRender(double delta)
    {
        if (Root is null || _surface is null) return;

        var canvas = _surface.Canvas;

        // Layout : calcule les positions et tailles de tous les Nodes
        Root.Layout(new Size(_window.Size.X, _window.Size.Y));

        // Efface le framebuffer puis demande à chaque Node de se dessiner
        canvas.Clear(SKColors.White);
        Root.Paint(canvas);

        // Flush soumet les commandes SkiaSharp au driver OpenGL
        canvas.Flush();

        Root.ClearDirty();
    }

    private void OnResize(Vector2D<int> _)
    {
        // La taille du framebuffer a changé — la surface doit être recrée
        // pour correspondre aux nouvelles dimensions.
        CreateSurface();
        Root?.MarkLayoutDirty();
    }

    private void OnClosing() => Root?.Dispose();

    public void Dispose()
    {
        _surface?.Dispose();
        _grContext?.Dispose();
        _window.Dispose();
    }
}
