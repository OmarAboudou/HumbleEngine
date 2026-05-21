# HumbleEngine — Architecture

> Décisions d'architecture arrêtées, plan d'implémentation ordonné.

---

## Contexte du projet

- **Langage** : C#
- **Scope v1** : moteur d'applications UI interactives, cross-platform (Desktop + Mobile)
- **Ambition long terme** : extensible vers un moteur de jeu complet (Node2D / Node3D)

---

## Décisions d'architecture

### D1 — Paradigme de représentation : Fork A (arbre mutable persistant)

Les nœuds sont des objets persistants avec identité. L'arbre est modifié directement via `AddChild` / `RemoveChild`. Les enfants d'un Node sont gérés manuellement — pas de réconciliation automatique.

**Pourquoi pas ECS** : contre-naturel pour les UI hiérarchiques.  
**Pourquoi pas Fork B (descriptions immuables + réconciliation)** : réconciliateur non trivial à implémenter, objectif est de démarrer rapidement.

---

### D2 — Réactivité : ReactiveProperty\<T\> + dirty flag via Subscribe

`ReactiveProperty<T>` est une classe simple. Elle ne connaît pas `Node`. Le câblage vers `MarkDirty()` se fait explicitement dans `Init()` via `Subscribe`.

```csharp
public class Button : Node
{
    public ReactiveProperty<string> Text     = new("Click");
    public ReactiveProperty<bool>   IsEnabled = new(true);

    public override void Init()
    {
        base.Init();
        Text.Subscribe(_ => MarkDirty());
        IsEnabled.Subscribe(_ => MarkDirty());
    }
}
```

Les ViewModels utilisent `ReactiveProperty<T>` librement, sans dépendance sur `Node`.

```csharp
public class CounterViewModel
{
    public ReactiveProperty<int> Count = new(0);
    public void Increment() => Count.Value++;
}
```

---

### D3 — Rendu : Layout + Paint dans le Node (Render Tree différé)

Dans un premier temps, chaque Node est responsable de son propre layout et de son propre rendu. Aucun Render Tree séparé.

```csharp
public abstract class Node
{
    public virtual void Layout(Size available) { }
    public virtual void Paint(SKCanvas canvas) { }
    public Rect ComputedBounds { get; protected set; }
}
```

**Discipline à respecter** : garder le code de rendu visuellement séparé du code logique dans chaque Node. L'extraction vers un Render Tree sera mécanique le moment venu.

**Render Tree (futur)** : quand introduit, chaque Node créera un `RenderObject` via `CreateRenderObject()`. Le RenderObject parent composera les RenderObjects de ses enfants — layout via contraintes descendantes, tailles remontantes.

---

### D4 — Séparation données/UI : MVVM

```
Model       →  données métier (C# pur)
ViewModel   →  ReactiveProperty<T>, logique de présentation
Node (View) →  structure UI, binding via Subscribe dans Init()
```

---

## Primitives — Design

### ReactiveProperty\<T\>

```csharp
public class ReactiveProperty<T>
{
    public T Value { get; set; }  // notifie les listeners si la valeur change

    public void Subscribe(Action<T> listener) { }
    public void Unsubscribe(Action<T> listener) { }
    public void BindFrom(ReactiveProperty<T> source) { }
}
```

- Pas d'`Owner`. Aucune dépendance sur `Node`.
- Égalité vérifiée avant notification — pas de boucles infinies sur `BindFrom`.
- `BindFrom` : binding sens unique, source → cette propriété.

---

### Signal / Signal\<T\>

Pattern `Create()` : une seule classe, la capacité d'émettre est retournée comme `Action`.

```csharp
public sealed class Signal
{
    private Signal() { }

    public static (Signal Signal, Action Emit) Create()
    {
        var s = new Signal();
        return (s, s.EmitCore);
    }

    private void EmitCore() { /* notifie les listeners */ }

    public void Connect(Action listener) { }
    public void Disconnect(Action listener) { }
}

public sealed class Signal<T>
{
    private Signal() { }

    public static (Signal<T> Signal, Action<T> Emit) Create()
    {
        var s = new Signal<T>();
        return (s, s.EmitCore);
    }

    private void EmitCore(T value) { /* notifie les listeners */ }

    public void Connect(Action<T> listener) { }
    public void Disconnect(Action<T> listener) { }
}
```

Usage dans un Node :

```csharp
public class Button : Node
{
    public Signal Pressed { get; }
    private readonly Action _emitPressed;

    public Button()
    {
        (Pressed, _emitPressed) = Signal.Create();
    }

    protected override void OnMouseUp(Vector2 pos) => _emitPressed();
}
```

`Emit` est une `Action` privée. Les consommateurs externes ne peuvent que `Connect` / `Disconnect`.

**Source Generators (futur)** : quand les patterns sont figés, un Source Generator peut générer le boilerplate depuis un attribut `[Signal]`.

---

### ReactiveCollection\<T\>

Signals fins pour les listes — évite de redessiner toute la liste pour un seul changement.

```csharp
public class ReactiveCollection<T> : IReadOnlyList<T>
{
    public Signal<(int Index, T Item)> ItemAdded   { get; }
    public Signal<(int Index, T Item)> ItemRemoved { get; }
    public Signal                      Reset        { get; }

    public void Add(T item) { }
    public void RemoveAt(int index) { }
    public void Clear() { }

    // IReadOnlyList<T>
    public T this[int index] { get; }
    public int Count { get; }
}
```

Câblage dans un Node :

```csharp
public override void Init()
{
    base.Init();
    Items.ItemAdded.Connect((_, _) => MarkDirty());
    Items.ItemRemoved.Connect((_, _) => MarkDirty());
    Items.Reset.Connect(() => MarkDirty());
}
```

---

### Node

```csharp
public abstract class Node
{
    // Arbre
    public Node? Parent { get; private set; }
    public IReadOnlyList<Node> Children { get; }

    public void AddChild(Node child)    // appelle child.Init() automatiquement
    public void RemoveChild(Node child) // appelle child.Dispose() automatiquement

    // Cycle de vie
    public virtual void Init() { }              // câbler les Subscribe ici
    public virtual void Update(float delta) { } // propagé aux enfants par défaut
    public virtual void Dispose() { }           // propagé aux enfants par défaut

    // Rendu
    public virtual void Layout(Size available) { }
    public virtual void Paint(SKCanvas canvas) { } // propagé aux enfants par défaut
    public Rect ComputedBounds { get; protected set; }

    // Dirty flag
    public void MarkDirty() { }
    public bool IsDirty { get; private set; }
    internal void ClearDirty() { }
}
```

**Propagation de MarkDirty** : marque uniquement le Node courant (pas de remontée vers la racine). Optimisation future.

**Init()** est appelé automatiquement par `AddChild`. C'est là que les `Subscribe` doivent être déclarés.

---

## Plan d'implémentation

### Phase 1 — Primitives *(aucune dépendance externe, testable avec NUnit)*

| # | Classe | Responsabilité |
|---|--------|---------------|
| 1 | `ReactiveProperty<T>` | Valeur réactive, listeners, `BindFrom` |
| 2 | `Signal` / `Signal<T>` | Communication événementielle, pattern `Create()` |
| 3 | `ReactiveCollection<T>` | Collection réactive, signals fins |
| 4 | `Node` | Arbre, cycle de vie, dirty flag |

### Phase 2 — Premier rendu *(quelque chose à l'écran)*

| # | Classe | Responsabilité |
|---|--------|---------------|
| 5 | `Application` | Fenêtre SkiaSharp, boucle principale (Update → Layout → Paint) |
| 6 | `Label` | Premier Node concret — affiche du texte |

### Phase 3 — Interaction

| # | Classe | Responsabilité |
|---|--------|---------------|
| 7 | Input system | Capture souris/clavier, distribution aux Nodes |
| 8 | Hit testing | Quel Node est sous le curseur (`ComputedBounds`) |
| 9 | `Button` | Signal `Pressed`, états hover/pressed |

### Phase 4 — Layout

| # | Classe | Responsabilité |
|---|--------|---------------|
| 10 | `Size`, `Rect`, `Constraints` | Structs de géométrie |
| 11 | `Column` | Empile les enfants verticalement |
| 12 | `Row` | Empile les enfants horizontalement |
| 13 | `Stack` | Superpose les enfants (z-order) |

### Phase 5 — MVVM complet

| # | Classe | Responsabilité |
|---|--------|---------------|
| 14 | `TextInput` | Saisie de texte, Signal `TextChanged` |
| 15 | Premier écran complet | ViewModel → bindings → Nodes |

---

## Extension vers le moteur de jeu (futur)

La base `Node` est conçue pour accueillir deux branches :

```
Node (base commune — lifecycle, signaux, arbre)
├── UINode     → Layout + Paint + Skia
└── GameNode   → Update chaque frame + pipeline 2D/3D
    ├── Node2D
    └── Node3D
```

Le rendu est délégué à des systèmes séparés — `Node` ne connaît pas le renderer.