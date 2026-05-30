# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commandes

```bash
# Compiler la solution
dotnet build HumbleEngine.sln

# Lancer le sample (point d'entrée utilisateur)
dotnet run --project HumbleEngine.Sample

# Compiler avec les fichiers générés visibles
# (EmitCompilerGeneratedFiles=true est déjà activé dans le .csproj)
# Les fichiers générés apparaissent dans HumbleEngine/obj/
```

## Structure des projets

- **HumbleEngine** — bibliothèque core (net10.0). Contient le moteur, le Node Tree, les Widgets, les Property/Signal, les abstractions de rendu, les passes (MountPass, LayoutPass, PaintPass).
- **HumbleEngine.Generators** — source generator Roslyn. Référencé comme `Analyzer` (pas de dépendance binaire). Génère le code des `[PrimitiveWidgetProperty]` et `[CompositeWidgetProperty]`.
- **HumbleEngine.Silk** — bibliothèque de fenêtrage (Dll, pas d'Exe). Fournit `SilkApplication` et `SilkWindow`. Dépend de Silk.NET. Ne dépend pas de Skia.
- **HumbleEngine.Skia** — renderer SkiaSharp. Implémente `IRenderer`. Dépend de SkiaSharp 3.x. Ne dépend pas de Silk.
- **HumbleEngine.Sample** — projet Exe d'exemple. Simule un projet utilisateur du moteur. Référence HumbleEngine + Silk + Skia. Contient `SampleApp : SilkApplication`.

## Architecture globale

### Couches principales

```
Application (Silk)
  └── Window (Node racine)
        └── Scene (Node Tree)
              └── UI (Node → Build() → Widget Tree)
                    └── Widget Tree (Mount → Layout → Paint)
```

### Node Tree

`HumbleObject` est la base de tout. `Node` hérite de `HumbleObject` et forme un arbre. Un `Node` a un `Parent` (Property) et des `Children` (ListProperty).

`UI` est un `Node` spécial qui expose un `Build()` retournant un `Widget`. C'est le pont entre le Node Tree et le Widget Tree.

### Widget Tree

Les Widgets sont des **records persistants** (contrairement à Flutter). Ils vivent tant que leur parent vit.

```
Widget (HumbleRecord abstrait)
├── PrimitiveWidget   — brique native : layout + paint. Porte RefreshFlags.
│   ├── PrimitiveSingleChildWidget  — un enfant via Child
│   └── PrimitiveMultiChildWidget   — n enfants via Children (ListProperty)
│
└── CompositeWidget   — se décompose via Build(). Porte IsDirty.
    ├── CompositeSingleChildWidget  — expose Child
    └── CompositeMultiChildWidget   — expose Children
```

La distinction fondamentale : un `PrimitiveWidget` est géré nativement par le moteur (layout/paint). Un `CompositeWidget` se décompose en primitives via `Build()`. Un composite finit **toujours** par se décomposer entièrement en primitives.

### Contrat Widget (méthodes publiques)

Toutes les méthodes suivantes sont définies sur `Widget` (base) et implémentées à chaque niveau :

- **`Layout(BoxConstraints)`** — abstrait sur `Widget`. `PrimitiveWidget` le laisse abstrait (chaque primitive l'implémente). `CompositeWidget` délègue à `MountedChildren[0].Layout()`.
- **`GetSize() → Size`** — public. `PrimitiveWidget` retourne `Size.Value`. `CompositeWidget` délègue à `MountedChildren[0].GetSize()` ou `Size(0,0)` si vide.
- **`SetLocalPosition(Position)`** — public. `PrimitiveWidget` écrit `LocalPosition.Value`. `CompositeWidget` délègue à `MountedChildren[0].SetLocalPosition()`.

**Règle** : dans `Layout()`, utiliser `MountedChildren` (arbre réconcilié) et non `Child.Value` (configuration brute). `MountedChildren` est peuplé par le reconcilier avant que layout tourne.

### Système de flags

`WidgetRefreshFlag` n'existe que sur les `PrimitiveWidget` via `RefreshFlags` :
- `LAYOUT` — déclenche `Layout(BoxConstraints)` sur le widget
- `PAINT` — redessine

Un `CompositeWidget` utilise `IsDirty` (booléen) à la place, qui déclenche un rappel de `Build()`.

### Passes internes (Mount → Layout → Paint)

Déclenchées chaque frame dans `Application.Run()` → `Rendering` callback, si `BuildRootWidget()` retourne un widget.

**`MountPass.Mount(widget)`** — naive (pas de diffing). Pour chaque widget :
- Si `CompositeWidget && IsDirty` : appelle `Build()`, stocke dans `BuiltSubTree`, remet `IsDirty = false`.
- Vide `MountedChildren`, repeuple depuis `GetChildren()`, fixe `Parent`, récurse.

**`LayoutPass.Layout(root, constraints)`** — appelle `root.Layout(constraints)`. Layout est récursif ; chaque widget appelle `Layout` sur ses `MountedChildren`.

**`PaintPass.Paint(root, buffer, parentOffset)`** — traverse l'arbre monté :
- `PrimitiveWidget` : `worldOffset = parentOffset + LocalPosition.Value` → `PaintBefore` → enfants → `PaintAfter`.
- `CompositeWidget` : transparent, parcourt `MountedChildren` en propageant `parentOffset`.

`PaintPass.DebugFill` (bool, `public static`) — si `true`, émet un `FillRect` coloré (palette par type) avant `PaintBefore` sur chaque primitive. Utile pour visualiser le layout.

### Paint

Le paint est piloté par une **PaintPass externe** — les widgets ne s'appellent pas entre eux. La pass traverse l'arbre récursivement :

```
Pour chaque PrimitiveWidget :
  1. primitive.PaintBefore(buffer, worldOffset)
  2. Traverser MountedChildren récursivement
  3. primitive.PaintAfter(buffer, worldOffset)
Pour chaque CompositeWidget :
  Traverser MountedChildren (transparent, aucune commande propre)
```

`PrimitiveWidget` expose deux hooks virtuels (vides par défaut) :
- **`PaintBefore(PaintCommandBuffer, Position)`** — avant les enfants (ex: fond, clip)
- **`PaintAfter(PaintCommandBuffer, Position)`** — après les enfants (ex: décoration foreground, restore clip)

Le `worldOffset` est la position absolue accumulée depuis la racine :
`worldOffset = parentWorldOffset + primitive.LocalPosition.Value`

### Système de rendu (PaintCommandBuffer)

Les widgets émettent des `PaintCommand` (records immuables) dans un `PaintCommandBuffer`. Le renderer (`IRenderer`) lit ce buffer et l'exécute.

Commandes disponibles :
- `FillRect`, `FillRRect`, `FillOval` — formes pleines
- `StrokeRRect` — contour
- `DrawShadow` — ombre portée (avec BlurRadius, SpreadRadius, OffsetX, OffsetY)
- `PushClipRect`, `PushClipRRect`, `PushClipOval` — clip avec save implicite (refermer avec `Pop`)
- `PushOpacity(byte)` — calque alpha (refermer avec `Pop`)
- `PushTransform(Matrix3x2)` — transformation (refermer avec `Pop`)
- `Pop` — restaure le dernier état sauvegardé

### IRenderer / SkiaRenderer

```csharp
public interface IRenderer : IDisposable
{
    bool Supports(GPUBackend backend);
    void Initialize(GPUBackend backend);  // appelé après création du contexte GPU
    void Render(PaintCommandBuffer buffer);
    void Resize(Size size);
}
```

`SkiaRenderer` (dans `HumbleEngine.Skia`) implémente `IRenderer` avec SkiaSharp 3.x. Il supporte OpenGL, Vulkan, Metal, Software. Pour OpenGL, il charge `libGL` via `NativeLibrary` et crée un `GRGlInterface`.

`Application` expose `protected virtual IRenderer? CreateRenderer() => null`. La sous-classe override pour retourner `new SkiaRenderer()`.

### ApplicationConfig

Sérialisable uniquement. Contient : `Title`, `Width`, `Height`, `PreferredBackend` (enum `GPUBackend`), `Scene` (Node?). Pas de lambdas, pas de passes, pas d'objets code.

### Application

```csharp
public abstract class Application
{
    protected abstract PlatformWindow CreatePlatformWindow(ApplicationConfig config);
    protected virtual IRenderer?  CreateRenderer()    => null;
    protected virtual Widget?     BuildRootWidget()   => null;   // temporaire — sera remplacé par le Node UI
    protected virtual IEnumerable<IUpdatePass>      GetCustomUpdatePasses()      => [];
    protected virtual IEnumerable<IFixedUpdatePass> GetCustomFixedUpdatePasses() => [];

    public void Run(ApplicationConfig config) { ... }
}
```

`CreatePlatformWindow(config)` est appelé via une factory dans `Run()`, après que la config est disponible. `SilkApplication` retourne `new SilkWindow(config.Width, config.Height)`.

`BuildRootWidget()` est provisoire : retourne la racine du widget tree à rendre. À terme, ce sera le système UI/Node qui fournira les racines.

### Source Generator

Deux attributs, deux comportements générés :

**`[PrimitiveWidgetProperty(flag)]`** — sur `PrimitiveWidget`. Connecte la property aux `RefreshFlags` via `CreateWidgetProperty`/`ConnectWidgetProperty`.

**`[CompositeWidgetProperty]`** — sur `CompositeWidget`. Connecte la property à `IsDirty = true` via `CreateCompositeProperty`/`ConnectCompositeProperty`.

```csharp
// Primitive : lève LAYOUT quand Child change
[PrimitiveWidgetProperty(WidgetRefreshFlag.LAYOUT | WidgetRefreshFlag.PAINT)]
public partial Property<Widget?> Child { get; init; }

// Composite : marque IsDirty quand Child change
[CompositeWidgetProperty]
public partial Property<Widget?> Child { get; init; }
```

### Reconciler

Travaille directement sur les Widgets (pas de structure séparée). `GetChildren()` est l'input (ce que le développeur a configuré), `MountedChildren` est la mémoire (état actuel de l'arbre).

Algorithme en deux phases : Phase 1 détermine le sort de chaque widget (réutilisé / déplacé / nouveau / disparu), Phase 2 applique les changements (Mount/Unmount). Les deux phases sont séparées pour éviter de démonter un widget qui a juste été déplacé.

**Règle critique** : `Build()` doit retourner les mêmes instances widget pour les mêmes widgets. Créer de nouvelles instances à chaque `Build()` sans raison fait perdre leur state au Reconciler.

### Property et Signal

`Property<T>` — observable réactive avec connexions faibles (WeakReference). Supporte bind unidirectionnel, bidirectionnel et avec transformation. `IPropertyListener<T>` est la vue en lecture seule exposée aux consommateurs.

`Signal` — événement one-shot. Connexions faibles. L'émetteur (`Action`) est exposé séparément via `out` au moment de la création dans `HumbleObject.CreateSignal()`.

### Système de clés (Reconciler)

- Sans clé : matching par référence d'instance
- `Key` : scoped à l'ancêtre avec `Key` le plus proche (sinon global à l'arbre)
- `GlobalKey` : identité absolue dans tout l'arbre

## Widgets implémentés

| Widget | Type | Statut |
|---|---|---|
| SizedBox | PrimitiveSingleChildWidget | ✅ |
| Padding | PrimitiveSingleChildWidget | ✅ |
| Align / Center | PrimitiveSingleChildWidget | ✅ |
| ConstrainedBox | PrimitiveSingleChildWidget | ✅ |
| DecoratedBox | PrimitiveSingleChildWidget | ✅ |
| ClipRect / ClipRRect / ClipOval | PrimitiveSingleChildWidget | ✅ |
| Opacity | PrimitiveSingleChildWidget | à faire |
| Transform | PrimitiveSingleChildWidget | à faire |
| FittedBox | PrimitiveSingleChildWidget | à faire |
| AspectRatio | PrimitiveSingleChildWidget | à faire |
| OverflowBox | PrimitiveSingleChildWidget | à faire |
| Column / Row | PrimitiveMultiChildWidget | à faire |
| Stack | PrimitiveMultiChildWidget | à faire |
| Container | CompositeWidget | à faire |
| Visibility | CompositeWidget | à faire |
| GestureDetector | PrimitiveSingleChildWidget | à faire |
| Expanded / Flexible | Pas un widget autonome | à faire |
| Positioned | Pas un widget autonome | à faire |

## Conventions importantes

- Les interfaces `ISingleChildWidget` / `IMultiChildWidget` sont `internal`. Les développeurs héritent des classes abstraites, pas de ces interfaces.
- `HumbleObject` (pour les classes) et `HumbleRecord` (pour les records) sont les bases symétriques — ils exposent les mêmes méthodes `CreatePublicProperty`, `CreateSignal`, etc.
- Le namespace UI est `HumbleEngine` (pas `HumbleEngine.UI` pour l'instant).
- Dans `Layout()`, toujours utiliser `MountedChildren` et non `Child.Value` directement.
- Les commandes `Push*` (clip, opacity, transform) doivent toujours être refermées par `Pop`.
- `Padding.Layout()` appelle `child.SetLocalPosition(new Position(insets.Left, insets.Top))` après le layout de l'enfant.
