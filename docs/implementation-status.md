# HumbleEngine — État d'implémentation

> Référence rapide de ce qui est implémenté et validé. Mis à jour après chaque phase.

---

## Phase 1 — Primitives ✅

Branche : `feature/phase-1-core-primitives`  
Tests : 46/46 ✅  
Dépendances externes : aucune

---

### `DirtyLevel` — `HumbleEngine/Core/DirtyLevel.cs`

```csharp
public enum DirtyLevel { None, Paint, Layout, Logic }
```

---

### `Node` — `HumbleEngine/Core/Node.cs`

```csharp
public abstract class Node
{
    // Arbre
    public Node? Parent { get; }
    public IReadOnlyList<Node> Children { get; }
    public void AddChild(Node child)    // appelle child.Init()
    public void RemoveChild(Node child) // appelle child.Dispose()

    // Cycle de vie
    public virtual void Init() { }
    public virtual void Update(float delta) { }  // propagé aux enfants
    public virtual void Layout(Size available) { } // propagé aux enfants
    public virtual void Dispose() { }             // propagé aux enfants

    // Dirty flag
    public DirtyLevel Dirty { get; }
    public bool IsDirty { get; }  // Dirty != None
    public void MarkDirty(DirtyLevel level)   // escalade uniquement
    public void MarkPaintDirty()              // → MarkDirty(Paint)
    public void MarkLayoutDirty()             // → MarkDirty(Layout)
    public void MarkLogicDirty()              // → MarkDirty(Logic)
    internal void ClearDirty()               // → Dirty = None

    // Rendu (Phase 2)
    public Rect ComputedBounds { get; protected set; }
}
```

**Règles :**
- `AddChild` lance une exception si le Node a déjà un parent
- `MarkDirty` n'escalade jamais vers le bas
- Câbler `MarkXxxDirty()` sur les `ReactiveProperty<T>` dans `Init()`

---

### `ReactiveProperty<T>` — `HumbleEngine/Core/ReactiveProperty.cs`

```csharp
public class ReactiveProperty<T> : ISignal<T>
{
    public ReactiveProperty(T initial)
    public T Value { get; set; }         // notifie si valeur change
    public void Connect(Action<T> listener)
    public void Disconnect(Action<T> listener)
    public void BindFrom(ReactiveProperty<T> source)  // binding sens unique
}
```

**Règles :**
- Implémente `ISignal<T>` — utilisable partout où `ISignal<T>` est attendu
- Égalité vérifiée avant notification — pas de boucle sur `BindFrom`
- Délègue la notification à un `MutableSignal<T>` interne

---

### `ISignal` / `ISignal<T>` — `HumbleEngine/Core/Signal.cs`

```csharp
public interface ISignal
{
    void Connect(Action listener);
    void Disconnect(Action listener);
}

public interface ISignal<T>
{
    void Connect(Action<T> listener);
    void Disconnect(Action<T> listener);
}
```

---

### `MutableSignal` / `Signal` — `HumbleEngine/Core/Signal.cs`

```csharp
// Côté émission — gardé privé dans le Node propriétaire
public sealed class MutableSignal : ISignal
{
    public Signal Signal { get; }     // vue publique, créée dans le constructeur
    public void Emit()
    public void Connect(Action listener)
    public void Disconnect(Action listener)
}

// Côté abonnement — exposé publiquement
public sealed class Signal : ISignal
{
    internal Signal(MutableSignal owner)
    public void Connect(Action listener)
    public void Disconnect(Action listener)
    // Emit() absent — cast Signal → MutableSignal impossible (types sans lien)
}

// Versions génériques identiques
public sealed class MutableSignal<T> : ISignal<T> { ... }
public sealed class Signal<T> : ISignal<T> { ... }
```

**Usage dans un Node :**
```csharp
private readonly MutableSignal _pressed = new();
public Signal Pressed => _pressed.Signal;
// → _pressed.Emit() en interne
```

---

### `ReactiveCollection<T>` — `HumbleEngine/Core/ReactiveCollection.cs`

```csharp
public class ReactiveCollection<T> : IReadOnlyList<T>
{
    // Signaux (exposés en lecture seule via Signal)
    public Signal<(int Index, T Item)> ItemAdded   { get; }
    public Signal<(int Index, T Item)> ItemRemoved { get; }
    public Signal                      Reset        { get; }
    public Signal                      Changed      { get; }  // agrégat

    // Mutation
    public void Add(T item)
    public void Insert(int index, T item)
    public bool Remove(T item)
    public void RemoveAt(int index)
    public void Clear()

    // IReadOnlyList<T>
    public T this[int index] { get; }
    public int Count { get; }
}
```

**Règles :**
- `Changed` est levé après chaque mutation (Add, Insert, RemoveAt, Clear)
- Les `MutableSignal` sont privés — les consommateurs reçoivent des `Signal`

---

### `Size` / `Rect` — `HumbleEngine/Core/Geometry.cs`

```csharp
public readonly record struct Size(float Width, float Height);
public readonly record struct Rect(float X, float Y, float Width, float Height)
{
    public bool Contains(float x, float y)
}
```

---

---

## Phase 2 — Premier rendu ✅

Branche : `feature/phase-2-rendering`  
Tests : 46/46 ✅ (Phase 1 inchangés — Phase 2 nécessite GPU)

---

### `Node` — ajout Phase 2

```csharp
// Nouveau dans Phase 2
public virtual void Paint(SKCanvas canvas)
// Propage aux enfants avec Save/Translate/Restore par enfant
```

---

### `Application` — `HumbleEngine/Application.cs`

```csharp
public sealed class Application : IDisposable
{
    public Node? Root { get; set; }
    public Application(string title = "HumbleEngine", int width = 800, int height = 600)
    public void Run()
    public void Dispose()
}
```

**Boucle interne (par frame) :**
1. `Root.Layout(windowSize)` — recalcule le layout
2. `canvas.Clear(White)` + `Root.Paint(canvas)` — redessine
3. `canvas.Flush()` + `Root.ClearDirty()`

**Stack technique :** Silk.NET (fenêtre + contexte OpenGL) + SkiaSharp (rendu GPU via GRContext).

---

### `Label` — `HumbleEngine/Nodes/Label.cs`

```csharp
public class Label : Node
{
    public ReactiveProperty<string>  Text     = new("");
    public ReactiveProperty<SKColor> Color    = new(SKColors.Black);
    public ReactiveProperty<float>   FontSize = new(16f);

    // Layout : mesure le texte → ComputedBounds.Width/Height
    // Paint  : DrawText à l'origine (parent gère la translation)
}
```

**Règles :**
- `Text` / `Color` → `MarkPaintDirty()`
- `FontSize` → `MarkLayoutDirty()`
- Le parent est responsable de placer `ComputedBounds.X/Y`

---

### `HumbleEngine.Sample`

Projet console de démonstration. Lance une fenêtre 800×600 avec un `Label`.

---

## Phases suivantes

| Phase | Contenu | Statut |
|-------|---------|--------|
| 3 | Input system, hit testing, `Button` | ⬜ |
| 4 | `Constraints`, `Column`, `Row`, `Stack` | ⬜ |
| 5 | `TextInput`, premier écran MVVM complet | ⬜ |
