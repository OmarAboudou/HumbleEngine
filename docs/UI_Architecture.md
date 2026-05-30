# Architecture UI — HumbleEngine

## Vue d'ensemble

L'architecture UI s'inspire de Flutter et React tout en s'adaptant aux contraintes d'un moteur de jeu en C#. Elle repose sur trois couches distinctes :

```
NodeTree          Widget Tree        Render
──────────        ────────────       ──────
UINode            Widget             Layout + Paint
(le "quoi")  →   (le "comment")  →  (le "où/taille")
```

- **UINode** — vit dans le NodeTree du moteur. C'est le point d'entrée. Il contient le state et produit l'arbre de Widgets via `Build()`.
- **Widget** — décrit l'UI de manière déclarative. Persistant entre les frames.
- **Layout + Paint** — calculé par les passes de rendu à partir des Widgets.

---

## Hiérarchie des Widgets

```
Widget (record abstrait)
├── PrimitiveWidget   → géré nativement par le moteur
│                       Column, Row, Stack, Rectangle, Text, Image,
│                       ClipRect, ClipOval, ClipPath, Conditional...
│
└── CompositeWidget   → se décrit via Build()
                        Button, Counter, Panel, HUD...
```

### Différence fondamentale

- Un `PrimitiveWidget` est une brique de base que le moteur sait gérer nativement (layout et/ou paint). Il peut avoir des enfants.
- Un `CompositeWidget` se décrit en combinant des Primitives et d'autres Composites via `Build()`. C'est l'équivalent d'un composant React.
- La distinction n'est pas "a des enfants ou non" mais "est-ce que le moteur sait le gérer directement ou est-ce que ça se décompose en autre chose".
- Un `CompositeWidget` finit toujours par se décomposer en `PrimitiveWidget` au bout de la chaîne de `Build()`.

### Widgets persistants

Contrairement à Flutter où les Widgets sont des snapshots éphémères et immuables, **nos Widgets sont persistants**. Ils vivent tant que leur parent vit. Cela élimine le besoin d'un Element Tree intermédiaire.

Les `Property<T>` sont lazy-initialized et partagées par référence lors des copies `with {}`, ce qui donne la persistance du state naturellement.

```csharp
// La Property<T> est créée au premier accès
// et partagée par référence lors d'un with {}
public partial Property<int> Count
{
    get => field ??= CreateWidgetProperty<int>(0);
    init { ... }
}
```

---

## Code C#

### WidgetRefreshFlag

`BUILD` n'existe plus en tant que flag — il est remplacé par `IsDirty` sur `CompositeWidget`. `WidgetRefreshFlag` ne concerne que les `PrimitiveWidget` :

```csharp
[Flags]
public enum WidgetRefreshFlag
{
    NONE   = 0,
    LAYOUT = 1 << 0, // PrimitiveWidget — recalcule taille et position
    PAINT  = 1 << 1, // PrimitiveWidget — redessine
}
```

### Widget

`Widget` ne contient que ce qui est commun aux deux types : les clés, la référence au parent, les enfants montés et les callbacks de cycle de vie.

```csharp
namespace HumbleEngine.UI;

public abstract partial record Widget
{
    // Clés
    public object? Key { get; init; }
    public object? GlobalKey { get; init; }

    // Référence au parent — maintenue par le Reconciler
    internal Widget? Parent { get; set; }

    // Mémoire du Reconciler — ce qui est réellement dans l'arbre
    // Distinct de GetChildren() qui représente ce que le développeur a configuré
    internal List<Widget> MountedChildren { get; } = [];

    // Input du Reconciler — ce que le développeur a configuré
    // Comparé à MountedChildren pour détecter les changements
    internal virtual IReadOnlyList<Widget> GetChildren() => [];

    // Callbacks de cycle de vie
    protected virtual void OnMount() { }
    protected virtual void OnUnmount() { }
}
```

### PrimitiveWidget

Contient tout ce qui concerne le layout et le paint :

```csharp
public abstract partial record PrimitiveWidget : Widget
{
    // Flags de rendu
    [WidgetProperty]
    public partial Property<WidgetRefreshFlag> RefreshFlag { get; internal init; }

    // Layout
    [WidgetProperty]
    public partial Property<Size> DesiredSize { get; internal init; }

    [WidgetProperty]
    internal partial Property<Size> Size { get; init; }

    [WidgetProperty]
    public partial Property<Position> LocalPosition { get; internal init; }

    [WidgetProperty]
    public partial Property<Position> ViewportPosition { get; internal init; }

    // Layout — à implémenter par chaque primitive
    public virtual void ComputeAndSetDesiredSize(BoxConstraints constraints) { }

    // Paint — à implémenter quand le Renderer sera défini
}

public abstract partial record PrimitiveSingleChildWidget : PrimitiveWidget, ISingleChildWidget<Widget>
{
    [WidgetProperty(WidgetRefreshFlag.LAYOUT | WidgetRefreshFlag.PAINT)]
    public partial Property<Widget?> Child { get; init; }

    internal override IReadOnlyList<Widget> GetChildren()
        => Child.Value is not null ? [Child.Value] : [];
}

public abstract partial record PrimitiveMultiChildWidget : PrimitiveWidget, IMultiChildWidget<Widget>
{
    [WidgetListProperty(WidgetRefreshFlag.LAYOUT | WidgetRefreshFlag.PAINT)]
    public partial ListProperty<Widget> Children { get; init; }

    internal override IReadOnlyList<Widget> GetChildren() => Children.Value;
}
```

### CompositeWidget

Ne contient que `IsDirty`, `BuiltSubTree` et `Build()`. Pas de layout, pas de paint — il se décompose en primitives.

```csharp
public abstract partial record CompositeWidget : Widget
{
    // Remplace le flag BUILD
    internal bool IsDirty { get; set; } = true;

    // Sous-arbre buildé — maintenu directement sur le widget
    // lu et mis à jour par le Reconciler uniquement
    internal Widget? BuiltSubTree { get; set; }

    // Doit toujours retourner les mêmes instances
    // sauf quand la structure change vraiment
    public abstract Widget Build();

    // Retourne le BuiltSubTree comme enfant unique
    internal override IReadOnlyList<Widget> GetChildren()
        => BuiltSubTree is not null ? [BuiltSubTree] : [];
}

public abstract partial record CompositeSingleChildWidget : CompositeWidget, ISingleChildWidget<Widget>
{
    public partial Property<Widget?> Child { get; init; }
}

public abstract partial record CompositeMultiChildWidget : CompositeWidget, IMultiChildWidget<Widget>
{
    public partial ListProperty<Widget> Children { get; init; }
}
```

### Interfaces internes enfants

Les interfaces sont `internal` — les développeurs étendent les classes abstraites plutôt que de les implémenter directement. Cela empêche d'implémenter les deux à la fois.

```csharp
internal interface ISingleChildWidget<TChild> where TChild : Widget
{
    Property<TChild?> Child { get; }
}

internal interface IMultiChildWidget<TChild> where TChild : Widget
{
    ListProperty<TChild> Children { get; }
}
```

**Distinction importante** : `Child`/`Children` sur ces interfaces représentent les **données configurées par le développeur**. `MountedChildren` sur `Widget` représente la **structure interne maintenue par le Reconciler**. Ce sont deux concepts distincts.

### UINode

```csharp
namespace HumbleEngine.UI;

public abstract class UINode : Node
{
    protected internal Widget? RootWidget { get; private set; }

    internal bool IsDirty { get; private set; } = true;

    public abstract Widget Build();

    protected internal Widget GetWidget()
    {
        if (RootWidget is null)
            RootWidget = Build();
        return RootWidget;
    }

    internal void Rebuild()
    {
        RootWidget = Build();
        IsDirty = false;
    }

    protected internal void ClearCache() => RootWidget = null;

    protected void MarkDirty() => IsDirty = true;

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        MarkDirty();
    }

    public override void Dispose()
    {
        ClearCache();
        base.Dispose();
    }
}
```

---

## Système de clés

Deux champs sur `Widget` :

```csharp
public object? Key { get; init; }       // clé locale
public object? GlobalKey { get; init; } // clé globale absolue
```

### Règles de matching

| Situation | Comportement |
|-----------|-------------|
| Pas de clé | Matching par référence d'instance |
| `Key` explicite | Scoped à l'ancêtre explicite le plus proche |
| Pas d'ancêtre avec `Key` | La `Key` devient globale à l'arbre entier |
| `GlobalKey` | Identité absolue dans tout l'arbre, ignore le scoping |

### GlobalKey implicite par chemin

Seules les `Key` explicites participent au chemin. Les widgets sans clé sont transparents :

```
UINode
  └── StylizedColumn (Key="sc")
       └── Padding (pas de clé → transparent)
            └── Child (Key="a")

GlobalKey implicite de Child = UINode / "sc" / "a"
```

Ce mécanisme permet au Reconciler de retrouver un Widget même s'il a été enveloppé dans un nouveau parent sans clé.

---

## Système de flags

### Connexion Property → Flag (PrimitiveWidget uniquement)

Chaque `Property<T>` d'un `PrimitiveWidget` déclare explicitement quel flag elle lève via `[WidgetProperty]` :

```csharp
[WidgetProperty(WidgetRefreshFlag.PAINT)]
public partial Property<Color> Color { get; init; }

[WidgetProperty(WidgetRefreshFlag.LAYOUT)]
public partial Property<float> Width { get; init; }
```

### IsDirty (CompositeWidget)

Un `CompositeWidget` n'a pas de `RefreshFlag`. Il a un `IsDirty` qui signale que `Build()` doit être rappelé. C'est le développeur qui appelle `MarkDirty()` ou qui connecte ses `Property<T>` manuellement.

### Propagation

```
CompositeWidget.IsDirty = true
→ Build() rappelé
→ Reconciler compare l'ancien et le nouvel arbre
→ Lève LAYOUT sur les PrimitiveWidgets affectés

PrimitiveWidget LAYOUT levé
→ ComputeAndSetDesiredSize() recalculé
→ Parent assigne Size définitive et LocalPosition
→ Si Size ou LocalPosition changé → lève PAINT
  (géré automatiquement via Property<T>)

PrimitiveWidget PAINT levé
→ Widget ajouté à la liste de repaint du Renderer
→ Tout le sous-arbre est repeint par-dessus
  (le parent est dessiné en premier, les enfants par-dessus)
```

### Layout — flow des contraintes

```
Parent → BoxConstraints → Enfant
Enfant → DesiredSize    → Parent
Parent → assigne Size définitive + LocalPosition à l'Enfant
```

Le parent a la vision globale (espace disponible, taille de tous ses enfants) donc c'est lui qui décide de la taille définitive.

---

## Passes de rendu

### UILayoutPass (Update loop)

```
Pour chaque UINode dans le NodeTree :
  Si IsDirty → Rebuild()

Parcourt le Widget Tree :
  CompositeWidget.IsDirty → Build() + Reconciler → reset IsDirty → lève LAYOUT
  PrimitiveWidget LAYOUT  → ComputeAndSetDesiredSize() → reset LAYOUT
```

La passe descend récursivement dans les `Build()` jusqu'à ne trouver que des `PrimitiveWidget`.

### UIPaintPass (Render loop)

```
Pour chaque UINode dans le NodeTree :
  Parcourt le Widget Tree :
    PrimitiveWidget PAINT levé → Paint() → reset PAINT
```

Les passes détectent les `UINode` directement par leur type, sans interface.

---

## Reconciler

### Responsabilité

Étant donné un ancien sous-arbre et un nouvel sous-arbre retournés par `Build()`, produire l'arbre final en réutilisant le maximum d'instances existantes.

### Représentation de l'arbre

Le Reconciler n'a pas de structure de données séparée. Il travaille directement sur les Widgets eux-mêmes :

- Les `PrimitiveWidget` portent leurs enfants via `GetChildren()`
- Les `CompositeWidget` portent leur sous-arbre buildé via `BuiltSubTree`

```
Pour chaque Widget :
  Si CompositeWidget et IsDirty :
    BuiltSubTree = Build()
    IsDirty = false

  Compare GetChildren() vs MountedChildren
  → Si différent → Réconcilie → met à jour MountedChildren → lève LAYOUT
  → Descend dans MountedChildren
```

`GetChildren()` unifie le traitement — le Reconciler l'appelle toujours de la même façon peu importe le type de Widget :
- `PrimitiveSingleChildWidget` → retourne `[Child.Value]`
- `PrimitiveMultiChildWidget` → retourne `Children.Value`
- `CompositeWidget` → retourne `[BuiltSubTree]`

`MountedChildren` est la **mémoire** du Reconciler (état actuel de l'arbre). `GetChildren()` est son **input** (ce que le développeur a configuré). La comparaison des deux permet de détecter les changements.

### Différence avec Flutter

Flutter compare deux arbres immuables. Notre Reconciler travaille sur des Widgets persistants — il réutilise les instances existantes plutôt que de les recréer.

### Algorithme en deux phases

**Phase 1 — Réconciliation (décision)**

Pour chaque widget du nouvel arbre, on détermine son sort :

```
1. A une GlobalKey explicite ?
   → Cherche dans GlobalKeyRegistry → "réutilisé" ou "déplacé"

2. A une Key explicite ?
   → Cherche dans le scope courant par clé
   → "réutilisé", "déplacé", ou "nouveau"

3. Pas de clé ?
   → Cherche par référence d'instance
   → "réutilisé" ou "nouveau"

4. Ancien widget non retrouvé dans le nouvel arbre ?
   → "disparu"
```

**Phase 2 — Application**

```
Widgets disparus   → Unmount() → OnUnmount() → désenregistre les clés
Widgets nouveaux   → Mount()   → OnMount()   → enregistre les clés
Widgets déplacés   → ni Mount ni Unmount, juste repositionnés
Widgets réutilisés → rien
```

On attend que toute la Phase 1 soit terminée avant d'appliquer la Phase 2 — cela évite de démonter par erreur un widget qui a juste été déplacé.

### GlobalKeyRegistry

Maintenu par la `UILayoutPass`. Indexe tous les widgets ayant une clé (explicite ou GlobalKey implicite par chemin) en temps constant.

```csharp
internal sealed class GlobalKeyRegistry
{
    private readonly Dictionary<object, Widget> _registry = new();

    internal void Register(object key, Widget widget) => _registry[key] = widget;
    internal void Unregister(object key) => _registry.Remove(key);
    internal Widget? Find(object key) => _registry.GetValueOrDefault(key);
}
```

### Règle fondamentale pour le développeur

> `Build()` doit toujours retourner les mêmes instances pour les mêmes widgets, sauf quand la structure change vraiment.

Si de nouvelles instances sont créées à chaque `Build()` sans raison, le Reconciler les traitera comme de nouveaux widgets et perdra leur state.

---

## Clipping

Le clipping est géré via des `PrimitiveWidget` dédiés :

```
ClipRect   → clippe selon un rectangle
ClipOval   → clippe selon une ellipse
ClipPath   → clippe selon un chemin arbitraire
```

Un `CompositeWidget` avec clipping implicite utilise ces primitives dans son `Build()` :

```csharp
public record SlideInWidget : CompositeWidget
{
    public override Widget Build() => new ClipRect(
        child: new AnimatedPosition(child: Child)
    );
}
```

---

## Widgets utilitaires notables

### Conditional

Permet de brancher entre deux widgets sans connecter `IsDirty` manuellement :

```csharp
public record Conditional : CompositeWidget
{
    public required Property<bool> Condition { get; init; }
    public required Widget OnTrue { get; init; }
    public required Widget OnFalse { get; init; }

    public override Widget Build()
        => Condition.Value ? OnTrue : OnFalse;
}
```

### CustomPaint

Trappe de secours pour du rendu custom :

```csharp
public record CustomPaint : PrimitiveWidget
{
    public Action<Canvas> Painter { get; init; }
    public Widget? Child { get; init; }

    public override void Paint(Canvas canvas) => Painter(canvas);
}
```

---

## Animations

Une animation est une `Property<T>` dont la valeur évolue dans le temps. Elle s'intègre naturellement avec les flags :

```csharp
// Sur un PrimitiveWidget
[WidgetProperty(WidgetRefreshFlag.LAYOUT)]
public partial Property<Position> AnimatedPosition { get; init; }
```

Le moteur d'animation fait évoluer les `Property<T>` à chaque frame. Les flags sont levés automatiquement, déclenchant layout et paint uniquement sur les widgets affectés.

---

## Décisions architecturales clés

| Décision | Raison |
|----------|--------|
| Widgets persistants | Élimine le besoin d'un Element Tree intermédiaire |
| `RefreshFlag` sur `PrimitiveWidget` uniquement | `BUILD` n'a pas de sens sur une primitive |
| `IsDirty` sur `CompositeWidget` au lieu de `BUILD` flag | Plus explicite, séparation claire des responsabilités |
| Flags explicites via `[WidgetProperty]` | Contrôle fin, performances optimales |
| Reconciler en deux phases | Évite de démonter des widgets déplacés par erreur |
| GlobalKey implicite par chemin | Gère les restructurations transparentes (ex: StylizedColumn) |
| Paint descend dans le sous-arbre | Le parent est dessiné en premier, les enfants par-dessus |
| Pas d'interface pour les passes | Les UINode sont détectés directement par leur type |
| `Build()` non pur assumé | Cohérent avec la persistance des widgets |
| CompositeWidget toujours décomposé en PrimitiveWidget | Seules les primitives ont un layout et un paint |
