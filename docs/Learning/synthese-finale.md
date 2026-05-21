# HumbleEngine — Synthèse finale

> Axe 1 + Axe 2 — Comparaisons et choix d'architecture
> Dernière mise à jour : Synthèse complète ✅

---

## Contexte du projet

**Langage cible** : C#
**Scope initial** : moteur d'applications UI interactives, cross-platform (desktop + mobile)
**Ambition long terme** : extensible vers un moteur de jeu complet

---

## Axe 1 — Comparaison des architectures moteur

| Critère | Godot — Node Tree | Unity — GameObject+Components | Unreal — Actor/Component | Bevy — ECS pur |
|--------|:-----------------:|:------------------------------:|:------------------------:|:--------------:|
| **Paradigme** | Arbre de Nodes spécialisés | Composition par empilement | Classes spécialisées + Components | Données / Logique strictement séparées |
| **Couplage données/logique** | Fusionnés dans le Node | Fusionnés dans MonoBehaviour | Fusionnés dans l'Actor | Complètement séparés |
| **Hiérarchie** | Arbre parent/enfant | Arbre parent/enfant | Flat + relations optionnelles | Flat (Archetypes) |
| **Communication** | Signaux découplés | GetComponent / UnityEvents | Delegates / RPCs | Events / Resources |
| **Réutilisabilité** | Scènes instanciables | Prefabs | Blueprints | Systèmes réutilisables |
| **Langage natif** | GDScript / C# | **C#** | C++ / Blueprint | Rust |
| **Adapté à l'UI** | Moyen | Moyen | Faible | Faible |
| **Adapté à C#** | ✅ Oui | ✅ Natif | ❌ C++ first | ❌ Rust |

### Ce qu'on retient de chaque moteur

| Moteur | Idée retenue |
|--------|-------------|
| **Godot** | Node Tree + Scenes instanciables + Signaux |
| **Unity** | Templates réutilisables (≈ Prefabs) |
| **Unreal** | Séparation logique de contrôle / représentation (Controller → Pawn) |
| **Bevy** | World comme conteneur global |

---

## Axe 2 — Comparaison des systèmes UI

| Critère | HTML + CSS | WPF / XAML | Flutter | Qt |
|--------|:---------:|:----------:|:-------:|:--:|
| **Paradigme** | Arbre + règles séparées | Arbre XAML + Data Binding | Tout est Widget | Widgets + Signals & Slots |
| **Pattern architectural** | — | MVVM | Stateless / Stateful | MVC |
| **Réactivité** | Via JavaScript | INotifyPropertyChanged | setState / InheritedWidget | Signals |
| **Langage** | HTML/CSS/JS | XAML + **C#** | Dart | C++ / QML |
| **Plateformes** | Web | Windows | **Toutes** | Tous OS desktop |
| **Rendu** | Navigateur | Composition Windows | Pixel par pixel (Skia) | Natif OS |

### Ce qu'on retient de chaque système UI

| Système | Idée retenue |
|---------|-------------|
| **HTML/CSS** | Séparation structure / apparence |
| **WPF/XAML** | MVVM + Commands + ResourceDictionaries |
| **Flutter** | **Architecture principale** — deux arbres, constraint-based layout, tout est Node, SkiaSharp, séparation Props/State |
| **Qt** | Signals & Slots comme système de communication événementielle |

---

## Architecture cible de HumbleEngine

### Vue d'ensemble

```
HumbleEngine
│
├── Node Tree              description de la scène et de l'UI
├── Render Tree            layout + paint (SkiaSharp)
├── Property<T>            réactivité unifiée
├── NodeState              état interne persisté par le moteur
├── Signal / Signal<T>     communication événementielle
├── MVVM                   séparation données / UI
└── Resource System        thèmes et styles
```

---

### 1. Node Tree — "Tout est Node"

Chaque élément de l'UI est un Node. Les Nodes sont **mutables** et organisés en arbre.

**Cycle de vie d'un Node :**

| Méthode | Quand | Rôle |
|---------|-------|------|
| Constructeur | À la création | Initialise les Properties |
| `Init()` | Après construction, appelé par le moteur | Set l'Owner sur les Properties via reflection |
| `Build()` | À l'entrée dans l'arbre + si `RequestRebuild()` | Construction du sous-arbre |
| `Update(delta)` | Chaque frame | Logique continue |
| `Dispose()` | À la destruction | Nettoyage, déconnexion des bindings |

> ⚠️ La reflection pour setter l'Owner des Properties doit se faire dans `Init()`, **pas dans le constructeur**. Les field initializers des sous-classes s'exécutent après le constructeur de la classe de base — les Properties n'existent pas encore quand `Node()` tourne.

---

### 2. Props vs State — la distinction fondamentale

C'est la distinction centrale de l'architecture. Elle détermine **qui est responsable du cycle de vie de la donnée**.

| | Props | State |
|--|-------|-------|
| **Où** | `Property<T>` directement sur le Node | `Property<T>` dans un `NodeState` |
| **Fourni par** | Le parent / un ViewModel | Le Node lui-même |
| **Si le Node est reconstruit** | Réinitialisé / rebindé depuis l'extérieur | **Survit** — persisté par le moteur |
| **Analogie Flutter** | Props du Widget | Objet State |
| **Analogie React** | Props | useState() |
| **Exemples** | `Text`, `Items`, `IsDisabled` | `IsHovered`, `ScrollPosition`, `IsExpanded` |

**Règle** : une donnée va dans le State si sa perte lors d'un rebuild serait une mauvaise expérience utilisateur.

```csharp
public class TextInput : Node
{
    // Props — fournies de l'extérieur, rebindées à chaque construction
    public Property<string> Text        { get; }
    public Property<string> Placeholder { get; }

    // Signals
    public Signal<string> TextChanged { get; } = new();
    public Signal         Submitted   { get; } = new();

    public TextInput()
    {
        // 'this' disponible dans le constructeur (pas dans les field initializers)
        Text        = new Property<string>("", this);
        Placeholder = new Property<string>("", this);
    }

    // State — interne, persisté par le moteur entre les rebuilds
    public override NodeState CreateState() => new TextInputState();

    protected class TextInputState : NodeState
    {
        public Property<bool> IsHovered  { get; } = new(false);
        public Property<bool> IsFocused  { get; } = new(false);
    }

    public override Node Build() { ... }
}
```

---

### 3. NodeState — persistance via Null Object Pattern

Tout Node expose `CreateState()`. Par défaut, il retourne `NodeState.Null` (singleton). Les Nodes avec état le surchargent pour retourner un vrai `NodeState`.

```csharp
public abstract class Node
{
    public virtual NodeState CreateState() => NodeState.Null;
}

public abstract class NodeState
{
    public static NodeState Null { get; } = new NullNodeState();
    private sealed class NullNodeState : NodeState { }
}
```

**Pas d'interface `IStateful`** — le Null Object Pattern suffit. Le moteur appelle `CreateState()` une fois et vérifie par référence :

```csharp
// Dans le moteur, lors du premier Build() d'un Node à une position
if (!_stateMap.TryGetValue(position, out var state))
{
    state = node.CreateState();  // appelé une seule fois
    if (!ReferenceEquals(state, NodeState.Null))
        _stateMap[position] = state;  // persisté uniquement si pas Null
}
// Si le Node est reconstruit → _stateMap[position] existe déjà → réutilisé
```

`CreateState()` n'est jamais appelé plus d'une fois par position. Pour les Nodes sans état, `NodeState.Null` est un singleton — aucune allocation.

---

### 4. Property\<T\> — réactivité unifiée

`Property<T>` est **le** concept central de réactivité. Il remplace `INotifyPropertyChanged`, les dirty flags manuels, et le système de binding.

```csharp
public class Property<T>
{
    private T _value;
    internal Node? Owner;  // setté par Init() via reflection
    private readonly List<WeakReference<Action<T>>> _listeners = new();

    public Property(T initial) => _value = initial;

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;
            _value = value;
            Owner?.MarkDirty();       // ← null-safe
            NotifyListeners(value);
        }
    }

    // Binding sens unique : source → cette Property
    public void BindFrom(Property<T> source, Func<T, T>? transform = null)
    {
        source.AddWeakListener(newValue =>
            Value = transform != null ? transform(newValue) : newValue);
        Value = transform != null ? transform(source.Value) : source.Value;
    }

    // Binding bidirectionnel
    public void BindTwoWay(Property<T> other)
    {
        BindFrom(other);
        other.BindFrom(this);
    }

    // Accès restreint
    public PropertyReader<T> AsReadOnly() => new(this);

    private void NotifyListeners(T value)
    {
        // Nettoyage des références mortes + notification
        _listeners.RemoveAll(wr => !wr.TryGetTarget(out _));
        foreach (var wr in _listeners)
            if (wr.TryGetTarget(out var listener))
                listener(value);
    }
}
```

> ⚠️ Les listeners sont stockés en **WeakReference** pour éviter les fuites mémoire. Si le Node destination est détruit, la référence vers le listener est collectée automatiquement.

**Les trois usages de `Property<T>` :**

| Usage | Où | Cycle de vie |
|-------|-----|-------------|
| Prop | Directement sur le Node | Réinitialisé à chaque rebuild |
| State | Dans un `NodeState` | Persisté par le moteur |
| ViewModel | Hors du Node Tree | Géré par l'application |

---

### 5. Signals — communication événementielle

```csharp
public class Signal
{
    private readonly List<Action> _listeners = new();
    public void Connect(Action listener)    => _listeners.Add(listener);
    public void Disconnect(Action listener) => _listeners.Remove(listener);
    internal void Emit()                    => _listeners.ForEach(l => l());
}

public class Signal<T>
{
    private readonly List<Action<T>> _listeners = new();
    public void Connect(Action<T> listener)    => _listeners.Add(listener);
    public void Disconnect(Action<T> listener) => _listeners.Remove(listener);
    internal void Emit(T value)                => _listeners.ForEach(l => l(value));
}
```

`Emit()` est `internal` — seul le moteur ou le Node propriétaire peut émettre.

Chaque Node déclare ses propres signaux :

```csharp
public class Button    : Node { public Signal         Pressed      { get; } = new(); }
public class TextInput : Node { public Signal<string> TextChanged  { get; } = new();
                                public Signal         Submitted    { get; } = new(); }
public class Slider    : Node { public Signal<float>  ValueChanged { get; } = new(); }
```

---

### 6. API fluente — collection initializer + méthodes On()

Les layout Nodes exposent `Add()` pour le collection initializer. Les méthodes fluentes retournent `this`.

```csharp
// NodeExtensions — méthodes génériques, fonctionnent avec n'importe quel Node/Signal
public static T On<T>(this T node, Func<T, Signal> selector, Action action) where T : Node
{
    selector(node).Connect(action);
    return node;
}

public static T On<T, TValue>(this T node, Func<T, Signal<TValue>> selector, Action<TValue> action) where T : Node
{
    selector(node).Connect(action);
    return node;
}

public static T Bind<T, TValue>(this T node, Func<T, Property<TValue>> selector, Property<TValue> source) where T : Node
{
    selector(node).BindTwoWay(source);
    return node;
}

public static T BindFrom<T, TValue>(this T node, Func<T, Property<TValue>> selector, Property<TValue> source) where T : Node
{
    selector(node).BindFrom(source);
    return node;
}
```

Usage — la hiérarchie est lisible par l'imbrication :

```csharp
public override Node Build()
{
    return new Column
    {
        new Row
        {
            new Label { Text = "Recherche :" },
            new TextInput()
                .Bind(n => n.Text, _vm.Query)
                .On(n => n.Submitted, _vm.Search)
        },
        new ListView()
            .BindFrom(n => n.Items, _vm.Results),
        new Button { Text = "Valider" }
            .On(n => n.Pressed, _vm.Submit)
    };
}
```

---

### 7. MVVM — Nodes sont la View

```
View     →  Node Tree
ViewModel →  Classe C# avec Property<T>   (pas d'INotifyPropertyChanged)
Model    →  Classe C# pure
```

```csharp
public class SearchViewModel
{
    public Property<string>       Query   { get; } = new("");
    public Property<List<string>> Results { get; } = new(new());

    public void Search() => Results.Value = FetchResults(Query.Value);
}
```

---

### 8. Trois niveaux de mise à jour

| Mécanisme | Coût GC | Usage |
|-----------|---------|-------|
| `property.Value = x` | **Nul** | Changement de donnée → dirty flag automatique |
| `RequestRebuild()` | Faible (pooling) | Changement structurel délibéré |

**Dirty flags par catégorie :**

| Flag | Propriétés concernées |
|------|-----------------------|
| `_transformDirty` | Position, taille, layout |
| `_styleDirty` | Couleur, bordure, opacité |
| `_contentDirty` | Texte, image, données affichées |

---

### 9. Responsive — résolution initiale + RequestRebuild explicite

`Build()` est appelé une fois avec la résolution initiale. Les changements de résolution déclenchent un `RequestRebuild()` **explicite** — jamais automatique.

```csharp
public class ResponsiveLayout : Node
{
    public override void Init()
    {
        base.Init();
        Engine.OnResolutionChanged += _ => RequestRebuild();
    }

    public override void Dispose()
    {
        Engine.OnResolutionChanged -= _ => RequestRebuild();
        base.Dispose();
    }

    public override Node Build() =>
        Engine.CurrentResolution.Width > 600
            ? new DesktopLayout()
            : new MobileLayout();
}
```

---

### 10. Render Tree et rendu cross-platform

```
Node Tree   →   description (Build())
                    ↓ dirty flags
Render Tree →   layout + paint (SkiaSharp)
```

SkiaSharp (portage .NET de Skia) : rendu pixel-perfect identique sur Windows, macOS, Linux, Android, iOS.

---

## Tableau complet des inspirations

| Concept | Inspiré de | Adaptation |
|---------|-----------|-----------|
| Tout est Node | Godot + Flutter | — |
| Node Tree | Godot | — |
| Render Tree (2 arbres) | Flutter | Simplifié de 3 à 2 |
| Constraint-based layout | Flutter | — |
| Props sur le Node | React / Flutter | `Property<T>` publiques |
| NodeState persisté | Flutter State | Via Null Object Pattern |
| `CreateState()` + Null Object | Flutter + Null Object Pattern | Pas d'interface `IStateful` |
| `Property<T>` | Kotlin StateFlow / Vue ref() | Unifie State + Binding + dirty flags |
| WeakReference dans listeners | — | Évite les fuites mémoire |
| `BindFrom()` / `BindTwoWay()` | WPF Binding | Sur `Property<T>` directement |
| `Init()` pour la reflection | — | Constructeur trop tôt pour les field initializers |
| Dirty flags | Godot | Déclenchés par `Property<T>` |
| `RequestRebuild()` | Godot `queue_redraw()` | Rebuild structurel explicite |
| SkiaSharp | Flutter / Skia | Portage .NET |
| Signals | Qt / Godot | `Signal` / `Signal<T>`, Emit() internal |
| API fluente `On()` / `Bind()` | — | Collection initializer + méthodes génériques |
| MVVM | WPF | ViewModel avec `Property<T>` |
| Commands | WPF | Signals connectés à des méthodes ViewModel |
| ResourceDictionary | WPF | Thèmes et styles centralisés |
| World | Bevy | Map Position → NodeState |
| Controller → Pawn | Unreal | Extensibilité vers le moteur de jeu |

---

## Quiz — Questions clés

- Quelle différence entre une Prop et un State ? Donne un exemple de chaque.
- Pourquoi la reflection pour setter l'Owner ne peut pas se faire dans le constructeur ?
- Pourquoi les listeners dans `Property<T>` sont-ils des `WeakReference` ?
- Comment le moteur sait-il qu'un Node a un State à persister, sans interface `IStateful` ?
- Que se passe-t-il si `RequestRebuild()` est appelé sur un Node qui a un `NodeState` ?
- Quelle différence entre `BindFrom()` et `BindTwoWay()` ? Dans quel cas utilise-t-on chacun ?
- Pourquoi `Emit()` est-il `internal` sur les Signals ?
