# HumbleEngine — État d'implémentation

> Référence rapide de ce qui est implémenté et validé. Mis à jour après chaque phase.
> Lire ce fichier en début de session avant de toucher au code.

---

## Structure du projet

```
HumbleEngine/
├── Application.cs
├── Reactivity/          Signal, ReactiveProperty, ReactiveCollection + interfaces
├── Scene/               Node, DirtyLevel, Geometry, HitTestFilter, HitTest
├── Rendering/           RenderNode, RenderNodeTree, RenderDescription
│   │                    BoxConstraints, LayoutData, IRenderNode, ICompositeRenderNode
│   │                    LayoutExtensions
│   └── Builders/
│       ├── Span/        Span, SpanData
│       ├── Box/         Box, BoxData
│       └── Column/      Column, ColumnData
└── Nodes/               Label, Button
```

Tous les types sont dans `namespace HumbleEngine;`.

---

## Phase 1 — Primitives ✅

Tests : 46/46 ✅ — aucune dépendance externe

---

### `DirtyLevel` — `Scene/DirtyLevel.cs`

```csharp
public enum DirtyLevel { None, Paint, Layout, Logic }
```

---

### `Node` — `Scene/Node.cs`

```csharp
public abstract class Node
{
    public Node? Parent { get; }
    public IReadOnlyList<Node> Children { get; }
    public void AddChild(Node child)     // appelle child.Init()
    public void RemoveChild(Node child)  // appelle child.Dispose()

    public virtual void Init() { }
    public virtual void Update(float delta) { }  // propagé aux enfants
    public virtual void Dispose() { }

    public DirtyLevel Dirty { get; }
    public bool IsDirty { get; }
    public void MarkDirty(DirtyLevel level)
    public void MarkPaintDirty()
    public void MarkLayoutDirty()
    public void MarkLogicDirty()
    internal void ClearDirty()

    // Input
    public virtual HitTestFilter MouseFilter => HitTestFilter.Ignore;
    public virtual void OnMouseEnter() { }
    public virtual void OnMouseLeave() { }
    public virtual void OnMouseDown()  { }
    public virtual void OnMouseUp()    { }
    public virtual void OnClick()      { }

    // Rendu — Template Method : Render() injecte Owner, RenderContent() est surchargé
    public Rect ComputedBounds { get; internal set; }  // écrit par RenderNodeTree après layout
    public RenderDescription Render() => RenderContent() with { Owner = this };
    protected virtual RenderDescription RenderContent() => RenderDescription.None;
}
```

**Règles :**
- `AddChild` lance une exception si le Node a déjà un parent
- `MarkDirty` n'escalade jamais vers le bas
- Câbler `MarkXxxDirty()` sur les `ReactiveProperty<T>` dans `Init()`
- Layout et Paint sont délégués au `RenderNodeTree` — ne pas les implémenter sur Node
- `ComputedBounds` est écrit par le RenderTree après la layout pass — utilisé par HitTest

---

### Hiérarchie `ReactiveProperty<T>` — `Reactivity/`

```csharp
public interface IReadOnlyReactiveProperty<out T> : ISignal<T>
{
    T Value { get; }
    ISignal<T, T> Reaffected { get; }  // (oldValue, newValue)
}

public interface IReactiveProperty<T> : IReadOnlyReactiveProperty<T>
{
    new T Value { get; set; }
}

public class ReactiveProperty<T> : IReactiveProperty<T>
{
    public ReactiveProperty(T initial)
    public T Value { get; set; }
    public ISignal<T, T> Reaffected { get; }
    public void Connect(Action<T> listener)
    public void Disconnect(Action<T> listener)
    public void BindFrom(ReactiveProperty<T> source)
    public static implicit operator T(ReactiveProperty<T> p)  // lecture implicite
}

public class ReadOnlyReactiveProperty<T> : IReadOnlyReactiveProperty<T>
{
    public ReadOnlyReactiveProperty(ReactiveProperty<T> source)
}
```

---

### Hiérarchie `Signal` — `Reactivity/Signal.cs`

```csharp
public interface ISignal
public interface ISignal<out T>
public interface ISignal<out T1, out T2>

public sealed class MutableSignal : ISignal        { public void Emit(); }
public sealed class MutableSignal<T> : ISignal<T>  { public void Emit(T value); }

public sealed class Signal : ISignal          { internal Signal(MutableSignal owner); }
public sealed class Signal<T> : ISignal<T>    { internal Signal(MutableSignal<T> owner); }
```

**Usage :**
```csharp
private readonly MutableSignal _pressed = new();
public Signal Pressed => _pressed.Signal;
```

---

### Hiérarchie `ReactiveCollection<T>` — `Reactivity/`

```csharp
public interface IReadOnlyReactiveCollection<out T> : IReadOnlyReactiveProperty<IReadOnlyList<T>>
{
    ISignal<int, T> ItemAdded   { get; }
    ISignal<int, T> ItemRemoved { get; }
    ISignal          Cleared     { get; }
    ISignal          Changed     { get; }
}

public class ReactiveCollection<T> : IReactiveCollection<T>
public class ReadOnlyReactiveCollection<T> : IReadOnlyReactiveCollection<T>
```

---

### `Size` / `Rect` — `Scene/Geometry.cs`

```csharp
public readonly record struct Size(float Width, float Height);
public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    public bool Contains(float x, float y)
}
```

---

## Phase 2 — Premier rendu ✅

---

### `Application` — `Application.cs`

```csharp
public sealed class Application : IDisposable
{
    public Node? Root { get; set; }
    public Application(string title = "HumbleEngine", int width = 800, int height = 600)
    public void Run()
    public void Dispose()
}
```

**Boucle par frame :**
1. `_renderTree.Rebuild(Root)` — Node.Render() → Flatten → tableaux plats
2. `_renderTree.Layout(BoxConstraints.Loose(windowSize))` — calcule les bounds
3. `canvas.Clear(White)` + `_renderTree.Paint(canvas)`
4. `canvas.Flush()` + `Root.ClearDirty()`

**Stack :** Silk.NET GLFW + SkiaSharp GRContext. `FramebufferSize` pour HiDPI.

---

### Render Tree — `Rendering/`

**Architecture :**
```
Node.RenderContent()  →  RenderDescription (arbre transitoire, Owner injecté par Render())
                               ↓ RenderNodeTree.Rebuild()
                    _nodes[] + _subtreeSizes[] + _layoutData[] + _owners[]
                    _spanData[] + _boxData[] + _columnData[]
                               ↓ RenderNodeTree.Layout(BoxConstraints)
                    bounds calculées → _nodes[i].Bounds + owner.ComputedBounds
                               ↓ RenderNodeTree.Paint()
                             SKCanvas
```

```csharp
// RenderNode — struct fixe dans le tableau plat
public readonly struct RenderNode
{
    public RenderNodeKind Kind   { get; init; }
    public int            Index  { get; init; }  // index dans _spanData[], _boxData[], etc.
    public Rect           Bounds { get; init; }  // bounds absolues, calculées par Layout()
}

public enum RenderNodeKind { None, Span, Box, Column }

// RenderDescription — valeur transitoire retournée par Node.Render()
public readonly struct RenderDescription
{
    public static readonly RenderDescription None = default;
    public static implicit operator RenderDescription(string content) => new Span(content);

    public RenderNodeKind       Kind     { get; internal init; }
    public Rect                 Bounds   { get; internal init; }
    public RenderDescription[]? Children { get; internal init; }
    public SpanData             Span     { get; internal init; }
    public BoxData              Box      { get; internal init; }
    public LayoutData           Layout   { get; internal init; }
    public ColumnData           Column   { get; internal init; }
    public Node?                Owner    { get; internal init; }
}

// RenderNodeTree
public sealed class RenderNodeTree
{
    public void Rebuild(Node root)
    public void Layout(BoxConstraints constraints)  // deux passes : descend contraintes, remonte tailles
    public void Paint(SKCanvas canvas)
    public IEnumerable<int> ChildIndices(int parentIndex)
}
```

**Navigation dans le tableau plat :**
- `_subtreeSizes[i]` = taille totale du sous-arbre à l'index `i` (soi inclus)
- Enfants de `i` : commencent à `i+1`, chaque frère est à `index + subtreeSizes[index]`

---

### Layout — `Rendering/`

```csharp
// Contraintes descendantes (parent → enfant)
public readonly struct BoxConstraints
{
    public float MinWidth, MaxWidth, MinHeight, MaxHeight;
    public static BoxConstraints Loose(Size available)      // min=0, max=available
    public static BoxConstraints Tight(Size size)           // min=max=size
    public static BoxConstraints Unconstrained              // max=∞
    public Size Constrain(float width, float height)        // clamp dans les contraintes
}

// Hints de sizing universels sur chaque RenderDescription
public readonly struct LayoutData
{
    public float? Width    { get; init; }  // null = hug content
    public float? Height   { get; init; }  // null = hug content
    public float  PaddingX { get; init; }
    public float  PaddingY { get; init; }
}

// Données spécifiques à Column
public readonly struct ColumnData
{
    public float Spacing { get; init; }
}
```

**Algorithme de layout par Kind :**
- `Span` → taille intrinsèque via `SKFont.MeasureText`, clampée dans les contraintes
- `Box` → layout des enfants superposés, taille = max enfant + padding
- `Column` → enfants empilés verticalement avec spacing, taille = somme + padding

---

### Builders déclaratifs — `Rendering/Builders/`

**Hiérarchie d'interfaces :**
```csharp
public interface IRenderNode                                    // tout builder
{
    LayoutData Layout { get; set; }
}

public interface ICompositeRenderNode                          // builders avec enfants
    : IRenderNode, IEnumerable<RenderDescription>
{
    void Add(RenderDescription child);
}
```

**Extension methods génériques — `LayoutExtensions.cs` :**
```csharp
// Disponibles sur tout T : struct, IRenderNode
T Width<T>(this T b, float? width)
T Height<T>(this T b, float? height)
T Padding<T>(this T b, float x, float y)
T Padding<T>(this T b, float uniform)
```

**Builders :**
```csharp
// Span — texte stylé (IRenderNode, feuille)
public struct Span : IRenderNode
{
    public Span(string content)                // contenu obligatoire
    public Span Color(SKColor color)
    public Span FontSize(float size)
    public RenderDescription At(Rect bounds)   // positionnement explicite
    public RenderDescription At(float x, float y)
    public static implicit operator RenderDescription(Span s)
}

// Box — conteneur avec fond (ICompositeRenderNode)
public struct Box : ICompositeRenderNode
{
    public Box Color(SKColor color)
    public Box CornerRadius(float radius)
    // + Width(), Height(), Padding() via LayoutExtensions
    public static implicit operator RenderDescription(Box b)
}

// Column — conteneur vertical (ICompositeRenderNode)
public struct Column : ICompositeRenderNode
{
    public Column Spacing(float spacing)
    // + Width(), Height(), Padding() via LayoutExtensions
    public static implicit operator RenderDescription(Column col)
}
```

**Usage dans RenderContent() :**
```csharp
// Label
protected override RenderDescription RenderContent()
    => new Span(Text.Value).Color(Color.Value).FontSize(FontSize.Value);

// Conteneur avec padding et enfants
protected override RenderDescription RenderContent() =>
    new Column
    {
        new Span("Titre").FontSize(24f),
        "Sous-titre",                       // string → RenderDescription
        new Box { child.Render() }.Color(SKColors.LightGray)
    }.Spacing(8).Padding(16f);
```

---

## Phase 3 — Input system ✅

---

### `HitTestFilter` — `Scene/HitTestFilter.cs`

```csharp
public enum HitTestFilter
{
    Ignore,   // hover uniquement, pas de click (défaut)
    Pass,     // hover + click, propagé vers les ancêtres
    Stop,     // hover + click, stoppé
    Disabled, // aucun événement
}
```

---

### `HitTest` — `Scene/HitTest.cs`

```csharp
public static class HitTest
{
    // Phase 1 — trouve le Node le plus profond au point (x,y), filtre ignoré
    public static Node? Find(Node root, float x, float y)

    // Phase 2 — dispatche un clic en remontant (Stop/Pass/Ignore)
    public static void DispatchClick(Node? node)

    // Chaîne d'ancêtres pour MouseEnter/Leave (exclut Disabled)
    public static List<Node> GetHoveredPath(Node? node)

    // Premier ancêtre interactif (Pass ou Stop) — cible du MouseDown
    public static Node? FirstInteractive(List<Node> path)
}
```

**Règles :**
- `Find` descend sans filtre — candidat le plus profond géographiquement
- `DispatchClick` remonte depuis la cible — filtre appliqué à la remontée
- `MouseEnter`/`Leave` : diff entre ancien et nouveau `GetHoveredPath`
- Clic confirmé : `_pressedNode` (FirstInteractive au MouseDown) encore dans le chemin au MouseUp

---

### `Button` — `Nodes/Button.cs`

```csharp
public class Button : Node
{
    public ReactiveProperty<string>  Text            = new("");
    public ReactiveProperty<SKColor> BackgroundColor = new(SKColor(220,220,220));
    public ReactiveProperty<SKColor> HoverColor      = new(SKColor(190,210,240));
    public ReactiveProperty<float>   FontSize        = new(16f);

    public Signal Pressed { get; }  // émis par OnClick()

    public override HitTestFilter MouseFilter => HitTestFilter.Stop;
    // OnMouseEnter/Leave → _isHovered + MarkPaintDirty
    // RenderContent : new Box { new Span(...) }.Color(bg).Padding(16f, 8f)
}
```

---

### `Label` — `Nodes/Label.cs`

```csharp
public class Label : Node
{
    public ReactiveProperty<string>  Text     = new("");
    public ReactiveProperty<SKColor> Color    = new(SKColors.Black);
    public ReactiveProperty<float>   FontSize = new(16f);
    // Text/FontSize → MarkLayoutDirty(), Color → MarkPaintDirty()
    // RenderContent : new Span(Text.Value).Color(Color.Value).FontSize(FontSize.Value)
}
```

---

## Phases suivantes

| Phase | Contenu | Statut |
|-------|---------|--------|
| 4 | `Column` Node, `Row` Node, tests layout | ⬜ |
| 5 | `TextInput`, premier écran MVVM complet | ⬜ |
