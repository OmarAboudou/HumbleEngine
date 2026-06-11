# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

HumbleEngine is a C# game engine built from scratch with the goal of understanding low-level systems. The focus is on learning by doing — P/Invoke over wrappers, direct OS API calls where possible. Target: Linux → Windows → macOS → Mobile.

- Language: C# / .NET 10.0
- IDE: Rider (`.idea/` present)
- Current phase: **Phase 5 — HAL implementation** (Bloc 1 designed, not yet coded)

## Build & run commands

```bash
# Build the entire solution
dotnet build HumbleEngine.sln

# Build a specific project
dotnet build HumbleEngine.Core/HumbleEngine.Core.csproj

# Run a project
dotnet run --project HumbleEngine.SomeProject/HumbleEngine.SomeProject.csproj

# Run tests (when they exist)
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

## Repository structure

The solution currently has one project; more will be added as phases progress:

```
HumbleEngine.Core/          — All abstractions (interfaces, abstract classes). No dependencies.
HumbleEngine.Linux/         — Linux platform assembly (deleted — being rewritten)
docs/
  roadmaps/roadmap_general.md     — 5-phase progression overview
  revisions/phase{1-5}_fiche_revision.md  — Per-phase reference sheets (concepts + code samples)
```

## Planned project structure (Phase 5 HAL)

```
Core                → interfaces only, no deps
HAL.Vulkan          → Core
HAL.OpenGL          → Core
HAL.Metal           → Core
HAL.D3D12           → Core
HAL.X11             → Core
HAL.Wayland         → Core
HAL.Win32           → Core
HAL.Cocoa           → Core
HAL.Linux           → Core + HAL.Vulkan + HAL.OpenGL + HAL.X11 + HAL.Wayland
HAL.Windows         → Core + HAL.D3D12 + HAL.Vulkan + HAL.Win32
HAL.macOS           → Core + HAL.Metal + HAL.Cocoa
```

The `Application` entry point references `Core` + the target platform assembly only.

## HAL architecture (active design — Phase 5 Bloc 1)

### Surface hierarchy

```
IGraphicsSurface : IDisposable
    bool ShouldClose
    event Action OnClose
    void Run(Action onFrame)

IWindow : IGraphicsSurface
    void Show() / Hide()
    void SetTitle(string)
    void Resize(int, int)
    event Action<int, int> OnResize

IMobileSurface : IGraphicsSurface
    event Action OnPause / OnResume / OnLowMemory
```

`Window` (abstract class) applies the Template Method pattern over `Run`:
```csharp
public void Run(Action onFrame)
{
    while (!ShouldClose) { PollEvents(); onFrame(); }
}
protected abstract void PollEvents();
```
`PollEvents` is an implementation detail — not on `IWindow`. On mobile, `Run` registers a callback and yields to the OS.

### Backend interfaces

```
ISurfaceBackend : IDisposable
    string Name
    void Initialize()
    IGraphicsSurface CreateSurface(SurfaceDescription)

IGraphicsBackend : IDisposable
    string Name
    IReadOnlyList<Type> CompatibleWindowBackends
    bool Supports(IWindowBackend)          // default impl : Contains(backend.GetType())
    void Initialize()                      // initialise l'API (VkInstance, device…)
    IRenderer CreateRenderer(IGraphicsSurface surface)

IRenderer : IDisposable
    void BeginFrame() / EndFrame() / Present()
```

Backend instances inside `OS` are **lightweight** — no system resources opened until `Initialize()` is called.

### OS descriptor

```csharp
abstract class OS
{
    public static OS Current { get; }   // auto-enregistré via [ModuleInitializer] dans l'assembly de plateforme
    public static void Register(OS os);
    public abstract string Name { get; }
    public abstract IReadOnlyList<IGraphicsBackend> AvailableGraphicsBackends { get; }
    public abstract IGraphicsBackend DefaultGraphicsBackend { get; }
    public IGraphicsBackend GetGraphicsBackend(string name);
}

abstract class DesktopOS : OS
{
    public abstract IReadOnlyList<IWindowBackend> AvailableWindowBackends { get; }
    public abstract IWindowBackend DefaultWindowBackend { get; }
    public IWindowBackend GetWindowBackend(string name);
    public IWindow CreateWindow(WindowDescription description, IWindowBackend? backend = null);
}

abstract class MobileOS : OS
{
    public abstract IMobileSurfaceBackend SurfaceBackend { get; }
}
```

### Surface descriptions

```csharp
record SurfaceDescription;
record WindowDescription(string Title, int Width, int Height,
    bool Resizable = true, bool Fullscreen = false) : SurfaceDescription;
record MobileSurfaceDescription(bool LockOrientation = false) : SurfaceDescription;
```

Each backend casts to the concrete type it expects and throws `ArgumentException` if mismatched.

### Lifecycle

```csharp
var desktopOS       = (DesktopOS)OS.Current;
var windowBackend   = desktopOS.DefaultWindowBackend;
var graphicsBackend = desktopOS.DefaultGraphicsBackend;

windowBackend.Initialize();
var window = windowBackend.CreateWindow(new WindowDescription("App", 1280, 720));

graphicsBackend.Initialize();
var renderer = graphicsBackend.CreateRenderer(window);

window.Run(() => { renderer.BeginFrame(); /* draw */ renderer.EndFrame(); renderer.Present(); });

// Destruction à la charge de l'application, ordre inverse de la création :
// renderer → graphicsBackend → window → windowBackend
```

Plusieurs fenêtres avec des backends différents sont parfaitement valides — l'application gère elle-même son cycle de vie, il n'y a pas de conteneur global imposé.

### Optional capabilities (AOT-safe)

```csharp
if (renderer is IRayTracingCapability rt) rt.TraceRays(desc);
if (renderer is ITileShadingCapability ts) ts.DispatchTileShader(desc);
```

### Internal native handle (hidden from application)

```csharp
internal interface INativeWindowHandle
{
    IntPtr GetNativeHandle();  // returns HWND, wl_surface*, NSWindow*, etc.
}
```

## Key design rules

- `Core` never references any backend directly — receives interfaces via injection.
- Only the platform assembly (`HAL.Linux`, `HAL.Windows`, `HAL.macOS`) and the application entry point know all backends.
- `PollEvents` must run on the main thread (X11, Win32, Cocoa mandate it) — enforced by `Window.Run`, hidden from `IWindow`.
- Backend compatibility (`CompatibleWindowBackends`) declared as `IReadOnlyList<Type>` sur `IGraphicsBackend` — `Supports(IWindowBackend)` a une implémentation par défaut dans l'interface, overridable si nécessaire.
- New renderer capabilities use `is` pattern, not interface inheritance, so backends don't need to implement unused capabilities.
- Pas d'`EngineContext` global — le cycle de vie est à la charge de l'application, ce qui permet plusieurs fenêtres avec des backends différents. Destruction dans l'ordre inverse de la création.

## Convention de namespace

- `HumbleEngine` — tout ce qui est dans `HumbleEngine.Core` (abstractions publiques)
- `HumbleEngine.Linux` — tout ce qui est dans `HumbleEngine.Linux`
- Les imports entre projets sont gérés via un `GlobalUsings.cs` par projet (pas de `using` répétés en tête de fichier).
