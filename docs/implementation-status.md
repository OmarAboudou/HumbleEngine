# HumbleEngine — État d'implémentation

> Référence rapide de ce qui est implémenté et validé. Mis à jour après chaque phase.

---

## Phase 1 — Primitives ✅

Branche : `feature/phase-1-core-primitives` → mergée dans `develop`
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
    public virtual void Update(float delta) { }    // propagé aux enfants
    public virtual void Layout(Size available) { } // propagé aux enfants
    public virtual void Paint(SKCanvas canvas) { } // propagé aux enfants avec Save/Translate/Restore
    public virtual void Dispose() { }              // propagé aux enfants

    // Dirty flag
    public DirtyLevel Dirty { get; }
    public bool IsDirty { get; }         // Dirty != None
    public void MarkDirty(DirtyLevel level)
    public void MarkPaintDirty()         // → MarkDirty(Paint)
    public void MarkLayoutDirty()        // → MarkDirty(Layout)
    public void MarkLogicDirty()         // → MarkDirty(Logic)
    internal void ClearDirty()          // → Dirty = None

    public Rect ComputedBounds { get; protected set; }
}
```

**Règles :**
- `AddChild` lance une exception si le Node a déjà un parent
- `MarkDirty` n'escalade jamais vers le bas
- Câbler `MarkXxxDirty()` sur les `ReactiveProperty<T>` dans `Init()`
- Le parent gère la translation canvas avant d'appeler `Paint` sur les enfants

---

### Hiérarchie `ReactiveProperty<T>` — `HumbleEngine/Core/`

```csharp
// Interfaces
public interface IReadOnlyReactiveProperty<out T> : ISignal<T>
{
    T Value { get; }
    ISignal<T, T> Reaffected { get; }  // (oldValue, newValue) à chaque changement
}

public interface IReactiveProperty<T> : IReadOnlyReactiveProperty<T>
{
    new T Value { get; set; }
}

// Implémentations
public class ReactiveProperty<T> : IReactiveProperty<T>
{
    public ReactiveProperty(T initial)
    public T Value { get; set; }
    public ISignal<T, T> Reaffected { get; }       // signal (oldValue, newValue)
    public void Connect(Action<T> listener)
    public void Disconnect(Action<T> listener)
    public void BindFrom(ReactiveProperty<T> source) // binding sens unique
    public static implicit operator T(ReactiveProperty<T> p) // lecture implicite
}

public class ReadOnlyReactiveProperty<T> : IReadOnlyReactiveProperty<T>
{
    public ReadOnlyReactiveProperty(ReactiveProperty<T> source)
    public T Value { get; }
    public ISignal<T, T> Reaffected { get; }
    public void Connect(Action<T> listener)
    public void Disconnect(Action<T> listener)
}
```

**Règles :**
- `IReadOnlyReactiveProperty<out T>` est covariant — `IReadOnlyReactiveProperty<Dog>` utilisable comme `IReadOnlyReactiveProperty<Animal>`
- `Reaffected` fournit les deux valeurs : utile pour transitions, undo/redo
- Délègue la notification à un `MutableSignal<T>` interne

---

### Hiérarchie `Signal` — `HumbleEngine/Core/Signal.cs`

```csharp
// Interfaces (covariantes)
public interface ISignal
public interface ISignal<out T>
public interface ISignal<out T1, out T2>

// MutableSignal — côté émission, gardé privé
public sealed class MutableSignal : ISignal
{
    public Signal Signal { get; }  // vue publique créée dans le constructeur
    public void Emit()
    public void Connect(Action) / Disconnect(Action)
}
public sealed class MutableSignal<T> : ISignal<T>   { public void Emit(T value) ... }
public sealed class MutableSignal<T1,T2> : ISignal<T1,T2> { public void Emit(T1, T2) ... }

// Signal — côté abonnement, exposé publiquement
public sealed class Signal : ISignal           { internal Signal(MutableSignal) }
public sealed class Signal<T> : ISignal<T>     { internal Signal(MutableSignal<T>) }
public sealed class Signal<T1,T2> : ISignal<T1,T2> { internal Signal(MutableSignal<T1,T2>) }
```

**Règles :**
- `Signal` et `MutableSignal` sont sans lien d'héritage — cast impossible, encapsulation structurelle
- `ISignal<out T>` est covariant grâce à la double contravariance de `Action<T>` en paramètre
- Constructeurs `internal Signal(...)` — seul le moteur peut créer des `Signal`

**Usage dans un Node :**
```csharp
private readonly MutableSignal _pressed = new();
public Signal Pressed => _pressed.Signal;
// → _pressed.Emit() en interne
```

---

### Hiérarchie `ReactiveCollection<T>` — `HumbleEngine/Core/`

```csharp
// Interfaces
public interface IReadOnlyReactiveCollection<out T> : IReadOnlyReactiveProperty<IReadOnlyList<T>>
{
    ISignal<int, T> ItemAdded   { get; }
    ISignal<int, T> ItemRemoved { get; }
    ISignal          Cleared     { get; }
    ISignal          Changed     { get; }
}

public interface IReactiveCollection<T>
    : IReadOnlyReactiveCollection<T>, IReactiveProperty<IReadOnlyList<T>>, IList<T>

// Implémentations
public class ReactiveCollection<T> : IReactiveCollection<T>
{
    // Signaux
    ISignal<int, T> ItemAdded   // index + item ajouté
    ISignal<int, T> ItemRemoved // index + item supprimé
    ISignal          Cleared     // liste vidée
    ISignal          Changed     // agrégat : fire après toute mutation

    // IList<T> — toutes les méthodes émettent les signaux appropriés
    // Indexeur setter : RemoveAt + Insert (émet ItemRemoved + ItemAdded)
    // Value : réaffectation émet ItemRemoved pour chaque ancien item, ItemAdded pour chaque nouveau
}

public class ReadOnlyReactiveCollection<T> : IReadOnlyReactiveCollection<T>
{
    public ReadOnlyReactiveCollection(IReactiveCollection<T> source)
    // Expose Value, Reaffected, ItemAdded, ItemRemoved, Cleared, Changed en lecture seule
}
```

**Règles :**
- `Changed` est dérivé — câblé sur `ItemAdded`/`ItemRemoved`/`Cleared` dans le constructeur, jamais émis manuellement
- `IReadOnlyReactiveCollection<out T>` est covariant
- Lors d'une réaffectation (`Value = newList`), `Reaffected` fire les items individuels de l'ancien et du nouveau

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

## Phase 2 — Premier rendu ✅

Branche : `feature/phase-2-rendering`
Tests : 46/46 ✅ (Phase 2 nécessite GPU — pas de tests unitaires)

---

### `Application` — `HumbleEngine/Application.cs`

```csharp
public sealed class Application : IDisposable
{
    public Node? Root { get; set; }
    public Application(string title = "HumbleEngine", int width = 800, int height = 600)
    public void Run()    // bloque jusqu'à fermeture
    public void Dispose()
}
```

**Boucle interne (par frame) :**
1. `Root.Layout(windowSize)`
2. `canvas.Clear(White)` + `Root.Paint(canvas)`
3. `canvas.Flush()` + `Root.ClearDirty()`

**Stack :** Silk.NET GLFW (fenêtre + contexte OpenGL) + SkiaSharp GRContext (rendu GPU).
`GRGlInterface.Create()` détecte le contexte GL courant. `FramebufferSize` (pixels physiques) pour HiDPI.

---

### `Label` — `HumbleEngine/Nodes/Label.cs`

```csharp
public class Label : Node
{
    public ReactiveProperty<string>  Text     = new("");
    public ReactiveProperty<SKColor> Color    = new(SKColors.Black);
    public ReactiveProperty<float>   FontSize = new(16f);
}
```

- `Text` / `Color` → `MarkPaintDirty()`
- `FontSize` → `MarkLayoutDirty()`
- `Layout` : `SKFont.MeasureText` → `ComputedBounds.Width/Height`
- `Paint` : `canvas.DrawText` à l'origine, y = `-font.Metrics.Ascent`

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
