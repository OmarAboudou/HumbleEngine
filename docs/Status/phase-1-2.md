# Phase 1 — Primitives ✅ + Phase 2 — Premier rendu ✅

Tests Phase 1 : 46/46 ✅ — Phase 2 requiert GPU, pas de tests unitaires

---

## `DirtyLevel` — `Scene/DirtyLevel.cs`

```csharp
public enum DirtyLevel { None, Paint, Layout, Logic }
```

---

## `Node` — `Scene/Node.cs`

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

    // Rendu — Template Method
    public Rect ComputedBounds { get; internal set; }  // écrit par RenderTree
    public RenderDescription Render() => RenderContent() with { Owner = this };
    protected virtual RenderDescription RenderContent() => RenderDescription.None;
}
```

**Règles :**
- `AddChild` lance une exception si le Node a déjà un parent
- Câbler `MarkXxxDirty()` sur les `ReactiveProperty<T>` dans `Init()`
- Layout et Paint délégués au `RenderTree` — ne pas les implémenter sur Node

---

## `ReactiveProperty<T>` — `Reactivity/`

```csharp
public class ReactiveProperty<T> : IReactiveProperty<T>
{
    public ReactiveProperty(T initial)
    public T Value { get; set; }
    public ISignal<T, T> Reaffected { get; }  // (oldValue, newValue)
    public void Connect(Action<T> listener)
    public void Disconnect(Action<T> listener)
    public void BindFrom(ReactiveProperty<T> source)
    public static implicit operator T(ReactiveProperty<T> p)
}
```

---

## `Signal` — `Reactivity/Signal.cs`

```csharp
public sealed class MutableSignal : ISignal { public void Emit(); }
public sealed class Signal : ISignal        { internal Signal(MutableSignal owner); }
// Idem pour MutableSignal<T>/Signal<T> et MutableSignal<T1,T2>/Signal<T1,T2>
```

Usage :
```csharp
private readonly MutableSignal _pressed = new();
public Signal Pressed => _pressed.Signal;
```

---

## `ReactiveCollection<T>` — `Reactivity/`

```csharp
public class ReactiveCollection<T> : IReactiveCollection<T>
// ISignal<int,T> ItemAdded, ItemRemoved — ISignal Cleared, Changed
```

---

## `Size` / `Rect` — `Scene/Geometry.cs`

```csharp
public readonly record struct Size(float Width, float Height);
public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    public bool Contains(float x, float y)
}
```

---

## `Application` — `Application.cs`

**Boucle par frame :**
1. `_renderTree.Rebuild(Root)` — Node.Render() → Flatten → tableaux plats
2. `_renderTree.Layout(BoxConstraints.Loose(windowSize))` — calcule les bounds
3. `canvas.Clear(White)` + `_renderTree.Paint(canvas)`
4. `canvas.Flush()` + `Root.ClearDirty()`

**Stack :** Silk.NET GLFW + SkiaSharp GRContext. `FramebufferSize` pour HiDPI.

---

## Render Tree — `Rendering/`

```csharp
public readonly struct RenderEntry
{
    public RenderEntryKind Kind   { get; init; }
    public int             Index  { get; init; }
    public Rect            Bounds { get; init; }
}

public enum RenderEntryKind { None, Span, Box, VLayout, HLayout }

public readonly struct RenderDescription
{
    public static implicit operator RenderDescription(string content) => new Span(content);
    public RenderEntryKind       Kind         { get; internal init; }
    public Rect                  Bounds       { get; internal init; }
    public RenderDescription[]?  Children     { get; internal init; }
    public SpanData              Span         { get; internal init; }
    public BoxData               Box          { get; internal init; }
    public LinearLayoutData      LinearLayout { get; internal init; }
    public LayoutData            Layout       { get; internal init; }
    public Node?                 Owner        { get; internal init; }
}

public sealed class RenderTree
{
    public void Rebuild(Node root)
    public void Layout(BoxConstraints constraints)
    public void Paint(SKCanvas canvas)
    public IEnumerable<int> ChildIndices(int parentIndex)
}
```

---

## `Label` — `Nodes/Label.cs`

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
