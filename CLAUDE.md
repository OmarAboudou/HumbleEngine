# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

HumbleEngine is a C# game engine built from scratch with the goal of understanding low-level systems. The focus is on learning by doing — P/Invoke over wrappers, direct OS API calls where possible. Target: Linux → Windows → macOS → Mobile. Cap produit : applications UI d'abord, puis l'éditeur du moteur (dogfooding), puis la 2D.

- Language: C# / .NET 10.0
- IDE: Rider (`.idea/` present)
- Current phase: **SceneGraph — cœur données + math** — voir `docs/roadmaps/04_scenegraph.md` pour l'avancement

## Build & run commands

```bash
# Build the entire solution
dotnet build HumbleEngine.sln

# Build a specific project
dotnet build HumbleEngine.HAL/HumbleEngine.HAL.csproj

# Run a project
dotnet run --project HumbleEngine.SomeProject/HumbleEngine.SomeProject.csproj

# Run all tests
dotnet test

# Run only unit tests (no display required)
dotnet test HumbleEngine.Tests/HumbleEngine.Tests.csproj

# Run Linux integration tests (requires DISPLAY)
dotnet test HumbleEngine.Tests.Linux/HumbleEngine.Tests.Linux.csproj

# Run a single test
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

## Repository structure

```
HumbleEngine.HAL/            — Abstractions uniquement (interfaces, classes abstraites). Aucune dépendance.
HumbleEngine.HAL.X11/        — Backend fenêtrage X11 (P/Invoke libX11)
HumbleEngine.HAL.Wayland/    — Backend fenêtrage Wayland (libdecor + fallback XDG brut)
HumbleEngine.HAL.Vulkan/     — Backend graphique Vulkan — instance, fallback multi-GPU, surface, device, swapchain, cycle de frame (clear)
HumbleEngine.HAL.OpenGL/     — Backend graphique OpenGL — GLX context, BeginFrame/EndFrame/Present
HumbleEngine.HAL.Linux/      — Assembly de plateforme Linux (agrège X11, Wayland, Vulkan, OpenGL)
HumbleEngine.Mathematics/    — Types mathématiques (Vector2, Rect, …) — autonome, aucune dépendance
HumbleEngine.SceneGraph/     — Node (hiérarchie, cycle de vie) ; Scene et SceneTree à venir
HumbleEngine.Sandbox/        — Projet exécutable de test — `dotnet run -- [Wayland|X11] [Vulkan|OpenGL]`
HumbleEngine.Tests/          — Tests unitaires — FakeOS, aucune dépendance à un display
HumbleEngine.Tests.Linux/    — Tests d'intégration — X11, GLX, Wayland, Vulkan, cycle frame complet
docs/
  roadmap_general.md              — Progression d'apprentissage par phase
  roadmaps/01_hal_implementation.md   — Vue d'ensemble HAL (statuts projets)
  roadmaps/02_extraction_x11_wayland.md — Historique : extraction X11/Wayland ✅
  roadmaps/03_vulkan_implementation.md  — Historique : implémentation Vulkan en 3 blocs ✅
  revisions/phase{1-5}_fiche_revision.md — Notes d'apprentissage (ne pas modifier)
```

## Dépendances entre projets

```
HAL                  → (aucune)
HAL.X11              → HAL
HAL.Wayland          → HAL
HAL.Vulkan           → HAL + HAL.X11 + HAL.Wayland
HAL.OpenGL           → HAL + HAL.X11 + HAL.Wayland
HAL.Linux            → HAL + HAL.X11 + HAL.Wayland + HAL.Vulkan + HAL.OpenGL
HAL.Windows (futur)  → HAL + HAL.Win32 + HAL.Vulkan + HAL.D3D12
HAL.macOS   (futur)  → HAL + HAL.Cocoa + HAL.Metal
Mathematics          → (aucune)
SceneGraph           → Mathematics
```

L'assembly de plateforme (`HAL.Linux`, `HAL.Windows`, `HAL.macOS`) est le seul à connaître tous les backends. L'application ne référence que `HAL` + l'assembly de plateforme cible, et enregistre explicitement l'OS via `OS.Register(new LinuxOS())` au démarrage.

## HAL architecture

### Surface hierarchy

```
IGraphicsSurface (IDisposable)
├── IWindow           — desktop: title, size, visibility
└── IMobileSurface    — mobile: pause, resume, memory pressure
```

`Window` (abstract) implements `IGraphicsSurface` via the Template Method pattern on `Run`.
`PollEvents` is a backend implementation detail — not exposed on `IWindow`.

### Backend hierarchy

```
ISurfaceBackend (IDisposable)
├── IWindowBackend         — desktop (X11, Wayland, Win32…)
└── IMobileSurfaceBackend  — mobile
IGraphicsBackend (IDisposable)  — independent (Vulkan, OpenGL, D3D12…)
```

Backend instances are **lightweight** — no system resources are opened until `Initialize()` is called.

### Lifecycle

```csharp
// The application is responsible for registering the target platform.
OS.Register(new LinuxOS());

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

## Key design rules

- `HAL` never references any backend directly — receives interfaces via injection.
- Un backend graphique identifie le système de fenêtrage via `IWindow.Backend` (jamais par cast du type concret de la fenêtre) et appelle `INativeWindowHandle.NotifyRendererAttached()` après avoir créé un renderer.
- Only the platform assembly (`HAL.Linux`, `HAL.Windows`, `HAL.macOS`) and the application entry point know all backends.
- `PollEvents` must run on the main thread (X11, Win32, Cocoa mandate it) — enforced by `Window.Run`, hidden from `IWindow`.
- Backend compatibility (`CompatibleWindowBackends`) declared as `IReadOnlyList<Type>` sur `IGraphicsBackend` — `Supports(IWindowBackend)` a une implémentation par défaut dans l'interface, overridable si nécessaire.
- New renderer capabilities use `is` pattern, not interface inheritance, so backends don't need to implement unused capabilities.
- Pas d'`EngineContext` global — le cycle de vie est à la charge de l'application, ce qui permet plusieurs fenêtres avec des backends différents. Destruction dans l'ordre inverse de la création.

## Convention de namespace

- **Le namespace reflète l'audience, pas l'assembly** (modèle `UnityEngine`/`Godot`).
- `HumbleEngine` (racine) — l'API publique du moteur, contribuée par plusieurs assemblies : `HumbleEngine.HAL` aujourd'hui, `Mathematics` et `SceneGraph` ensuite.
- Namespaces suffixés (`HumbleEngine.Linux`, `.X11`, `.Wayland`, `.Vulkan`, `.OpenGL`) — la plomberie plateforme, utilisée au bootstrap ou en interne.
- Les imports entre projets sont gérés via un `GlobalUsings.cs` par projet (pas de `using` répétés en tête de fichier).
