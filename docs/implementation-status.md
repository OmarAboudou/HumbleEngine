# HumbleEngine — État d'implémentation

> Référence rapide de ce qui est implémenté et validé. Mis à jour après chaque phase.
> Lire ce fichier en début de session avant de toucher au code.

---

## Structure du projet

```
HumbleEngine/
├── Application.cs
├── Reactivity/          Signal, ReactiveProperty, ReactiveCollection + interfaces
├── Scene/               Node, DirtyLevel, Geometry
├── Rendering/           RenderNode, RenderNodeTree, RenderDescription, TextData
│   └── Builders/        Text, Column (briques déclaratives)
└── Nodes/               Label (et futurs Nodes built-in)
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
    public virtual void Update(float delta) { }    // propagé aux enfants
    public virtual void Layout(Size available) { } // propagé aux enfants
    public virtual void Paint(SKCanvas canvas) { } // propagé avec Save/Translate/Restore
    public virtual void Dispose() { }

    public DirtyLevel Dirty { get; }
    public bool IsDirty { get; }
    public void MarkDirty(DirtyLevel level)
    public void MarkPaintDirty()
    public void MarkLayoutDirty()
    public void MarkLogicDirty()
    internal void ClearDirty()

    public Rect ComputedBounds { get; protected set; }
    public virtual RenderDescription Render() => RenderDescription.None;
}
```

**Règles :**
- `AddChild` lance une exception si le Node a déjà un parent
- `MarkDirty` n'escalade jamais vers le bas
- Câbler `MarkXxxDirty()` sur les `ReactiveProperty<T>` dans `Init()`
- `Render()` retourne la description visuelle — injecter `ComputedBounds` dans la description

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

**Règles :**
- `IReadOnlyReactiveProperty<out T>` est covariant
- `Reaffected` fournit oldValue + newValue — utile pour transitions, undo/redo
- Délègue la notification à un `MutableSignal<T>` interne

---

### Hiérarchie `Signal` — `Reactivity/Signal.cs`

```csharp
public interface ISignal
public interface ISignal<out T>           // covariant
public interface ISignal<out T1, out T2>  // covariant

public sealed class MutableSignal : ISignal        { public void Emit(); }
public sealed class MutableSignal<T> : ISignal<T>  { public void Emit(T value); }
public sealed class MutableSignal<T1,T2> : ISignal<T1,T2> { public void Emit(T1, T2); }

public sealed class Signal : ISignal          { internal Signal(MutableSignal owner); }
public sealed class Signal<T> : ISignal<T>    { internal Signal(MutableSignal<T> owner); }
public sealed class Signal<T1,T2> : ISignal<T1,T2> { internal Signal(MutableSignal<T1,T2> owner); }
```

**Règles :**
- `Signal` et `MutableSignal` sont sans lien d'héritage — cast impossible, encapsulation structurelle
- Constructeurs `internal Signal(...)` — seul le moteur crée des `Signal`
- `ISignal<out T>` covariant grâce à la double contravariance de `Action<T>` en paramètre

**Usage :**
```csharp
private readonly MutableSignal _pressed = new();
public Signal Pressed => _pressed.Signal;
// émettre : _pressed.Emit()
// s'abonner : node.Pressed.Connect(() => ...)
```

---

### Hiérarchie `ReactiveCollection<T>` — `Reactivity/`

```csharp
public interface IReadOnlyReactiveCollection<out T> : IReadOnlyReactiveProperty<IReadOnlyList<T>>
{
    ISignal<int, T> ItemAdded   { get; }
    ISignal<int, T> ItemRemoved { get; }
    ISignal          Cleared     { get; }
    ISignal          Changed     { get; }  // agrégat
}

public interface IReactiveCollection<T>
    : IReadOnlyReactiveCollection<T>, IReactiveProperty<IReadOnlyList<T>>, IList<T>

public class ReactiveCollection<T> : IReactiveCollection<T>
public class ReadOnlyReactiveCollection<T> : IReadOnlyReactiveCollection<T>
```

**Règles :**
- `Changed` est dérivé — câblé sur les trois autres signaux dans le constructeur
- Réaffectation via `Value = newList` : émet `ItemRemoved` pour chaque ancien item, `ItemAdded` pour chaque nouveau
- `IReadOnlyReactiveCollection<out T>` est covariant

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

Tests Phase 1 : 46/46 ✅ — Phase 2 requiert GPU, pas de tests unitaires

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
1. `Root.Layout(windowSize)`
2. `_renderTree.Rebuild(Root)` — construit le Render Tree depuis `Node.Render()`
3. `canvas.Clear(White)` + `_renderTree.Paint(canvas)`
4. `canvas.Flush()` + `Root.ClearDirty()`

**Stack :** Silk.NET GLFW + SkiaSharp GRContext. `GRGlInterface.Create()` sans lambda. `FramebufferSize` pour HiDPI.

---

### Render Tree — `Rendering/`

**Architecture :**
```
Node.Render()  →  RenderDescription (arbre transitoire)
                        ↓ RenderNodeTree.Rebuild()
               RenderNode[] plat + int[] subtreeSizes + TextData[] + ...
                        ↓ RenderNodeTree.Paint()
                      SKCanvas
```

```csharp
// RenderNode — struct fixe 24 bytes dans le tableau plat
public readonly struct RenderNode
{
    public RenderNodeKind Kind   { get; init; }  // quel type
    public int            Index  { get; init; }  // index dans _textData[], _boxData[], etc.
    public Rect           Bounds { get; init; }  // bounds absolues
}

public enum RenderNodeKind { None, Text, Box, Column }

// RenderDescription — valeur transitoire retournée par Node.Render()
public readonly struct RenderDescription
{
    public RenderNodeKind       Kind     { get; internal init; }
    public Rect                 Bounds   { get; internal init; }
    public RenderDescription[]? Children { get; internal init; }
    public TextData             Text     { get; internal init; }
}

// TextData — données typées stockées dans _textData[]
public readonly struct TextData
{
    public string  Content  { get; init; }
    public SKColor Color    { get; init; }
    public float   FontSize { get; init; }
}

// RenderNodeTree
public sealed class RenderNodeTree
{
    public void Rebuild(Node root)    // root.Render() → Flatten()
    public void Paint(SKCanvas canvas)
    public IEnumerable<int> ChildIndices(int parentIndex)  // navigation par subtreeSize
}
```

**Navigation dans le tableau plat :**
- `_subtreeSizes[i]` = taille totale du sous-arbre à l'index `i` (soi inclus)
- Enfants de `i` : commencent à `i+1`, chaque frère est à `index + subtreeSizes[index]`

---

### Builders déclaratifs — `Rendering/Builders/`

Briques de rendu built-in. Utilisateurs du moteur ne peuvent pas en créer de nouvelles.

```csharp
// Leaf
public readonly struct Text
{
    public Text(string content, SKColor color = default, float fontSize = 16f)
    public static implicit operator RenderDescription(Text t)
}

// Conteneur — collection initializer
public struct Column
{
    public void Add(RenderDescription child)
    public static implicit operator RenderDescription(Column col)
}
```

**Usage dans Node.Render() :**
```csharp
public override RenderDescription Render()
{
    RenderDescription desc = new Text(Text.Value, Color.Value, FontSize.Value);
    return desc with { Bounds = ComputedBounds };
}

// Ou avec conteneur :
public override RenderDescription Render() =>
    (RenderDescription) new Column
    {
        new Text("TITRE"),
        _children[0].Render()
    } with { Bounds = ComputedBounds };
```

---

### `Label` — `Nodes/Label.cs`

```csharp
public class Label : Node
{
    public ReactiveProperty<string>  Text     = new("");
    public ReactiveProperty<SKColor> Color    = new(SKColors.Black);
    public ReactiveProperty<float>   FontSize = new(16f);
    // Text/Color → MarkPaintDirty(), FontSize → MarkLayoutDirty()
    // Layout : SKFont.MeasureText → ComputedBounds.Width/Height
    // Render : new Text(...) with { Bounds = ComputedBounds }
}
```

---

## Phases suivantes

| Phase | Contenu | Statut |
|-------|---------|--------|
| 3 | Input system, hit testing, `Button` | ⬜ |
| 4 | `Constraints`, `Column` Node, `Row`, `Stack` | ⬜ |
| 5 | `TextInput`, premier écran MVVM complet | ⬜ |
