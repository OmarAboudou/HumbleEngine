# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

HumbleEngine is a C# game engine built from scratch with the goal of understanding low-level systems. The focus is on learning by doing — P/Invoke over wrappers, direct OS API calls where possible. Target: Linux → Windows → macOS → Mobile. Cap produit : applications UI d'abord, puis l'éditeur du moteur (dogfooding), puis la 2D.

- Language: C# / .NET 10.0
- IDE: Rider (`.idea/` present)
- Current phase : **aucune en cours — prochaine à choisir.** Dernière terminée : **les signals** (`docs/roadmaps/12_signals.md`) — `Reactive<T>`→`Property<T>` (rôle vs mécanisme), `AsReadOnly` (lecture seule garantie par le type), auto-tracking `Effect`/`Computed` (les lectures listent leurs dépendances), intégration nœud `CreateEffect`/`CreateComputed`. Suivi noté : les collections réactives (rebrancher le `LinearContainer`). Candidates suite : l'éditeur (dogfooding), la 2D. **Différé** : le découplage pompe/rendu (`docs/roadmaps/11_boucle.md`, attend l'éditeur / vrai multi-fenêtre ; Sandbox réduit à une fenêtre). Avant les signals : **le texte** (`docs/roadmaps/10_texte.md`, 6 blocs).

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
HumbleEngine.HAL/            — Abstractions uniquement (interfaces, classes abstraites) + contrat de dessin (Vertex, IMesh). Dépend de Mathematics seulement.
HumbleEngine.HAL.X11/        — Backend fenêtrage X11 (P/Invoke libX11)
HumbleEngine.HAL.Wayland/    — Backend fenêtrage Wayland (libdecor + fallback XDG brut)
HumbleEngine.HAL.Vulkan/     — Backend graphique Vulkan — instance (validation en Debug), fallback multi-GPU, swapchain, dynamic rendering, pipelines + shaders SPIR-V (compilés au build), meshes (CreateMesh/Draw), quads UI (übershader, push constants, DrawQuad)
HumbleEngine.HAL.OpenGL/     — Backend graphique OpenGL — GLX context, BeginFrame/EndFrame/Present
HumbleEngine.HAL.Linux/      — Assembly de plateforme Linux (agrège X11, Wayland, Vulkan, OpenGL)
HumbleEngine.Mathematics/    — Types mathématiques (Vector2, Rect, …) — autonome, aucune dépendance
HumbleEngine.Reactive/       — Runtime de réactivité (Property&lt;T&gt; + AsReadOnly, ObservableList&lt;T&gt;, bindings, auto-tracking : Effect/Computed) — autonome, aucune dépendance
HumbleEngine.SceneGraph/     — Node (fermé par défaut), VisualNode (OnDraw), UINode/Panel/Column/Row (pixels, layout réactif), SelectableText (sélection/copie partagées) → Label (se mesure → sizing par contenu, sélectionnable) et TextField (champ éditable : caret, édition, couper/coller, two-way binding), SceneTree (hooks de cycle de vie, QueueDispose, Render, DefaultFontAtlas + Clipboard injectés), Scene, NodeSlot/NodeList observables, BindItemsFrom
HumbleEngine.Text/           — Rendu de texte — FreeType en P/Invoke (FreeTypeNative), Font (face mémoire, rastérisation, métriques), GlyphAtlas (pré-cuit, R8, shelf-packing), Glyph, TextLayout (mise en forme une ligne). Police DejaVu Sans embarquée. Dépend de HAL + Mathematics
HumbleEngine.Sandbox/        — Projet exécutable de test — démo mono-fenêtre (Wayland + Vulkan) : un trio, triangle/panneaux/colonne/texte/champs éditables, `dotnet run` sans argument. (Le multi-fenêtre attend le découplage pompe/rendu, roadmap 11 différée.)
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
HAL                  → Mathematics
HAL.X11              → HAL
HAL.Wayland          → HAL
HAL.Vulkan           → HAL + HAL.X11 + HAL.Wayland + Mathematics
HAL.OpenGL           → HAL + HAL.X11 + HAL.Wayland
HAL.Linux            → HAL + HAL.X11 + HAL.Wayland + HAL.Vulkan + HAL.OpenGL
HAL.Windows (futur)  → HAL + HAL.Win32 + HAL.Vulkan + HAL.D3D12
HAL.macOS   (futur)  → HAL + HAL.Cocoa + HAL.Metal
Mathematics          → (aucune)
Reactive             → (aucune)
SceneGraph           → HAL + Mathematics + Reactive + Text
Text                 → HAL + Mathematics
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
- Boucle en trois étages composables : `IWindow.PollEvents` (primitive de pompe, contrat documenté : main thread), `IGraphicsSurface.Step(onFrame)` (une itération, retourne true tant que la surface continue — idiome MoveNext), `Run` (sucre mono-surface, default interface method composée sur Step). Multi-fenêtre = un Step par fenêtre dans la condition du while, la politique de boucle appartient à l'application.
- Backend compatibility (`CompatibleWindowBackends`) declared as `IReadOnlyList<Type>` sur `IGraphicsBackend` — `Supports(IWindowBackend)` a une implémentation par défaut dans l'interface, overridable si nécessaire.
- New renderer capabilities use `is` pattern, not interface inheritance, so backends don't need to implement unused capabilities.
- Pas d'`EngineContext` global — le cycle de vie est à la charge de l'application, ce qui permet plusieurs fenêtres avec des backends différents. Destruction dans l'ordre inverse de la création.
- Trio « une fenêtre ↔ un renderer ↔ un arbre » (`SceneTree(renderer)`). Les nœuds sont défaut-constructibles (contrat éditeur) : ressources GPU acquises à l'attach, libérées au detach, via le point d'accès unique `VisualNode.Renderer` (évolution WindowNode/viewport notée en roadmap 09).

## Convention de namespace

- **Le namespace reflète l'audience, pas l'assembly** (modèle `UnityEngine`/`Godot`).
- `HumbleEngine` (racine) — l'API publique du moteur, contribuée par plusieurs assemblies : `HumbleEngine.HAL`, `Mathematics`, `Reactive` et `SceneGraph`.
- Namespaces suffixés (`HumbleEngine.Linux`, `.X11`, `.Wayland`, `.Vulkan`, `.OpenGL`) — la plomberie plateforme, utilisée au bootstrap ou en interne.
- Les imports entre projets sont gérés via un `GlobalUsings.cs` par projet (pas de `using` répétés en tête de fichier).
