# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commandes

```bash
# Compiler la solution
dotnet build HumbleEngine.sln

# Lancer l'application (point d'entrée Silk)
dotnet run --project HumbleEngine.Silk

# Compiler avec les fichiers générés visibles
# (EmitCompilerGeneratedFiles=true est déjà activé dans le .csproj)
# Les fichiers générés apparaissent dans HumbleEngine/obj/
```

## Structure des projets

- **HumbleEngine** — bibliothèque core (net10.0). Contient le moteur, le Node Tree, les Widgets, les Property/Signal.
- **HumbleEngine.Generators** — source generator Roslyn. Référencé comme `Analyzer` (pas de dépendance binaire). Génère le code des `[PrimitiveWidgetProperty]` et `[CompositeWidgetProperty]`.
- **HumbleEngine.Silk** — implémentation de la fenêtre et point d'entrée. Dépend de Silk.NET.

## Architecture globale

### Couches principales

```
Application (Silk)
  └── Window (Node racine)
        └── Scene (Node Tree)
              └── UI (Node → Build() → Widget Tree)
                    └── Widget Tree (Reconciler → Layout → Paint)
```

### Node Tree

`HumbleObject` est la base de tout. `Node` hérite de `HumbleObject` et forme un arbre. Un `Node` a un `Parent` (Property) et des `Children` (ListProperty).

`UI` est un `Node` spécial qui expose un `Build()` retournant un `Widget`. C'est le pont entre le Node Tree et le Widget Tree.

`Application.Run()` boucle sur des `IFixedUpdatePass` et `IUpdatePass` enregistrés dans `ApplicationConfig`. Ces passes parcourent le Node Tree et déclenchent layout, reconciliation et paint.

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

### Système de flags

`WidgetRefreshFlag` n'existe que sur les `PrimitiveWidget` via `RefreshFlags` :
- `LAYOUT` — déclenche `Layout(BoxConstraints)` sur le widget
- `PAINT` — redessine

Un `CompositeWidget` utilise `IsDirty` (booléen) à la place, qui déclenche un rappel de `Build()`.

### Layout

`PrimitiveWidget.Layout(BoxConstraints)` est la méthode abstraite à implémenter sur chaque primitive. Elle reçoit les contraintes du parent, calcule la taille et l'écrit dans `Size.Value`. Le parent lit `child.Size.Value` après l'appel et assigne `child.LocalPosition.Value`.

Il n'y a pas de `DesiredSize` séparé — `Size` est la seule source de vérité pour la taille d'un widget.

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

## Conventions importantes

- Les interfaces `ISingleChildWidget` / `IMultiChildWidget` sont `internal`. Les développeurs héritent des classes abstraites, pas de ces interfaces.
- `HumbleObject` (pour les classes) et `HumbleRecord` (pour les records) sont les bases symétriques — ils exposent les mêmes méthodes `CreatePublicProperty`, `CreateSignal`, etc.
- Le namespace UI est `HumbleEngine` (pas `HumbleEngine.UI` pour l'instant).
- L'architecture détaillée des Widgets est dans `docs/UI_Architecture.md`.
