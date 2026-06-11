# Fiche de révision — Phase 5 : Multi-plateforme & production
 
> Progression : ██░░░░░░░░ 1/3 blocs en cours
 
---
 
## ✅ Bloc 1 — HAL (Hardware Abstraction Layer)
*3 passages effectués — maîtrisé*
 
### Problème résolu
 
Sans HAL, le code applicatif contient des `if/else` plateforme partout. Le HAL expose une API unique et cache les backends concrets derrière des interfaces.
 
```
Application
    ↓
  Core  (IGraphicsSurface, IRenderer, OS...)
    ↓
HAL.Linux / HAL.Windows / HAL.macOS  (backends concrets)
```
 
---
 
### Structure des projets
 
```
Core               → rien  (contient toutes les abstractions)
HAL.Vulkan         → Core
HAL.OpenGL         → Core
HAL.Metal          → Core
HAL.D3D12          → Core
HAL.X11            → Core
HAL.Wayland        → Core
HAL.Win32          → Core
HAL.Cocoa          → Core
HAL.Linux          → Core + HAL.Vulkan + HAL.OpenGL + HAL.X11 + HAL.Wayland
HAL.Windows        → Core + HAL.D3D12 + HAL.Vulkan + HAL.Win32
HAL.macOS          → Core + HAL.Metal + HAL.Cocoa
Application        → Core + HAL.{plateforme cible}
```
 
`Core` ne connaît jamais les backends directement — il reçoit des interfaces via injection. Seul le point d'entrée (`Application`) connaît tous les backends et les assemble.
 
Les abstractions vivent dans `Core` pour l'instant. Les extraire dans `HAL.Abstractions` est une refactorisation mécanique si le besoin se présente.
 
---
 
### Hiérarchie des interfaces
 
**Surface graphique**
 
```
IGraphicsSurface : IDisposable
    ├── bool ShouldClose
    ├── event Action OnClose
    └── void Run(Action onFrame)
 
IWindow : IGraphicsSurface
    ├── void Show() / Hide()
    ├── void SetTitle(string)
    ├── void Resize(int, int)
    └── event Action<int, int> OnResize
 
IMobileSurface : IGraphicsSurface
    ├── event Action OnPause
    ├── event Action OnResume
    └── event Action OnLowMemory
```
 
**Classe abstraite `Window`** — applique le patron Template Method sur `Run` :
 
```csharp
abstract class Window : IWindow
{
    public void Run(Action onFrame)
    {
        while (!ShouldClose)
        {
            PollEvents();   // ← implémenté par chaque backend
            onFrame();
        }
    }
 
    protected abstract void PollEvents();
}
```
 
`PollEvents` est un détail d'implémentation — pas sur `IWindow`. Sur mobile, `Run` enregistre un callback et rend la main à l'OS (pas de boucle `while`).
 
---
 
**Descriptions de surface**
 
```csharp
record SurfaceDescription;
 
record WindowDescription(
    string Title,
    int Width,
    int Height,
    bool Resizable  = true,
    bool Fullscreen = false
) : SurfaceDescription;
 
record MobileSurfaceDescription(
    bool LockOrientation = false
) : SurfaceDescription;
```
 
Chaque backend caste vers le type concret qu'il attend :
 
```csharp
// WaylandBackend
public IGraphicsSurface CreateSurface(SurfaceDescription desc)
{
    var windowDesc = desc as WindowDescription
        ?? throw new ArgumentException("Wayland requires WindowDescription");
    // ...
}
```
 
---
 
**Backends**
 
```
ISurfaceBackend : IDisposable          ← base commune
    ├── string Name
    └── void Initialize()              ← idempotent, ouvre la connexion

IWindowBackend : ISurfaceBackend       ← desktop
    └── IWindow CreateWindow(WindowDescription)

IMobileSurfaceBackend : ISurfaceBackend  ← mobile
    └── IMobileSurface GetSurface(MobileSurfaceDescription)

IGraphicsBackend : IDisposable
    ├── string Name
    ├── IReadOnlyList<Type> CompatibleWindowBackends   ← types concrets (ex: typeof(X11WindowBackend))
    ├── bool Supports(IWindowBackend)                  ← implémentation par défaut dans l'interface
    ├── void Initialize()                              ← initialise l'API GPU (VkInstance, device…)
    └── IRenderer CreateRenderer(IGraphicsSurface)     ← lie le renderer à une surface précise
```
 
Les instances dans `OS` sont **non initialisées** — légères, pas de ressource système ouverte. `Initialize()` est appelé explicitement par l'application.
 
---
 
**Renderer**
 
```
IRenderer : IDisposable
    ├── void BeginFrame()
    ├── void EndFrame()
    └── void Present()
    (détaillé lors de l'implémentation Vulkan)
```
 
---
 
**Capacités optionnelles** — détectées via `is`, AOT-safe :
 
```csharp
if (renderer is IRayTracingCapability rt) rt.TraceRays(desc);
if (renderer is ITileShadingCapability ts) ts.DispatchTileShader(desc);
```
 
---
 
**Handle natif** — interne au HAL, invisible à l'application :
 
```csharp
internal interface INativeWindowHandle
{
    IntPtr GetNativeHandle();  // retourne HWND, wl_surface, NSWindow...
}
```
 
---
 
### OS — descripteur de plateforme
 
Trois niveaux d'abstraction :
 
```csharp
abstract class OS
{
    public static OS Current { get; }   // auto-enregistré via [ModuleInitializer]
    public static void Register(OS os); // appelé une seule fois par l'assembly de plateforme
    public abstract string Name { get; }
    public abstract IReadOnlyList<IGraphicsBackend> AvailableGraphicsBackends { get; }
    public abstract IGraphicsBackend DefaultGraphicsBackend { get; }
    public IGraphicsBackend GetGraphicsBackend(string name);
}
 
abstract class DesktopOS : OS           // Linux, Windows, macOS
{
    public abstract IReadOnlyList<IWindowBackend> AvailableWindowBackends { get; }
    public abstract IWindowBackend DefaultWindowBackend { get; }
    public IWindowBackend GetWindowBackend(string name);
    public IWindow CreateWindow(WindowDescription description, IWindowBackend? backend = null);
}
 
abstract class MobileOS : OS            // iOS, Android
{
    public abstract IMobileSurfaceBackend SurfaceBackend { get; }
}
```
 
```csharp
sealed class LinuxOS : DesktopOS
{
    public override string Name => "Linux";
    public override IReadOnlyList<IWindowBackend> AvailableWindowBackends { get; }
        = [new WaylandWindowBackend(), new X11WindowBackend()]; // Wayland préféré
    public override IReadOnlyList<IGraphicsBackend> AvailableGraphicsBackends { get; }
        = [new VulkanGraphicsBackend(), new OpenGLGraphicsBackend()];
    public override IWindowBackend   DefaultWindowBackend   => AvailableWindowBackends[0];
    public override IGraphicsBackend DefaultGraphicsBackend => AvailableGraphicsBackends[0];
}
```
 
`LinuxModuleInit` enregistre `LinuxOS` via `[ModuleInitializer]` — l'application n'a pas à le faire manuellement, il suffit de référencer `HumbleEngine.Linux`.
 
---
 
### Cycle de vie complet
 
```csharp
var desktop  = (DesktopOS)OS.Current;
var winBack  = desktop.DefaultWindowBackend;
var gfxBack  = desktop.DefaultGraphicsBackend;
 
// 1. Initialiser les backends (ouvre connexion X11/Wayland, crée VkInstance…)
winBack.Initialize();
gfxBack.Initialize();
 
// 2. Créer la fenêtre
var window = winBack.CreateWindow(new WindowDescription("Mon App", 1280, 720));
 
// 3. Créer le renderer lié à cette fenêtre (swapchain, framebuffers…)
var renderer = gfxBack.CreateRenderer(window);
 
// 4. Boucle principale
window.Run(() =>
{
    renderer.BeginFrame();
    // draw calls
    renderer.EndFrame();
    renderer.Present();
});
 
// 5. Destruction — ordre inverse de la création, à la charge de l'application
renderer.Dispose();
gfxBack.Dispose();
window.Dispose();
winBack.Dispose();
```
 
Pas d'`EngineContext` global — l'application gère son propre cycle de vie, ce qui permet plusieurs fenêtres avec des backends différents simultanément.
 
---
 
### Pièges & décisions
 
| Sujet | Décision |
|---|---|
| `static abstract Name` | Propriété d'instance — `static abstract` inaccessible via instance lors de l'itération |
| Échec d'`Initialize` | Exception — l'appelant fait le fallback (`try/catch` sur un autre backend) |
| Cycle de vie | Pas d'`EngineContext` global — la destruction est à la charge de l'application, dans l'ordre inverse |
| `PollEvents` | Main thread obligatoire (X11, Win32, Cocoa l'exigent) — détail d'implémentation caché dans `Window` |
| Backends supportés | Propriété immuable dans l'OS — pas de registry dynamique |
| Compatibilité windowing/graphics | `CompatibleWindowBackends : IReadOnlyList<Type>` + `Supports(IWindowBackend)` avec implémentation par défaut dans l'interface |
| Un renderer par surface | `CreateRenderer(IGraphicsSurface)` — `BeginFrame/Present` n'ont pas de paramètre de surface, donc le renderer est intrinsèquement lié à une surface |
| `SurfaceDescription` | `record` — immuable, extensible (`WindowDescription`, `MobileSurfaceDescription`) |
 
---
 
*Dernière mise à jour : Bloc 1 HAL ✓*