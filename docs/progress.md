# HumbleEngine — Progress

## Ce qui est implémenté

### Core (`HumbleEngine/`)

#### Système réactif
- **`Signal`**, **`Signal<T>`**, **`Signal<T1,T2>`** — système d'événements avec Connect/Disconnect/Emit
- **`ReadOnlySignal`** — vue en lecture seule d'un signal (Connect/Disconnect seulement)
- **`IReadOnlySignal`** — interfaces correspondantes
- **`Property<T>`** — propriété réactive avec `ValueChanged : IReadOnlySignal<T,T>` (old, new)
- **`ReadOnlyProperty<T>`** — vue en lecture seule
- **`ListProperty<T>`** — liste réactive avec signaux `Added` et `Removed`
- **`ReadOnlyListProperty<T>`** — vue en lecture seule avec accès aux signaux
- Fix : `Signal.Emit` itère sur un snapshot pour éviter InvalidOperationException si un callback modifie les connexions pendant l'émission

#### Arbre de scène
- **`Node`** — nœud abstrait avec :
  - Relation parent/enfant via `Property<Node?> _parent` et `ListProperty<Node> _children`
  - `SetParent()`, `Attach()`, `Detach()`
  - Lifecycle : `OnTreeEntered`, `OnTreeExited`, `OnChildrenEntered`, `OnChildrenExited` (définis, pas encore wirés)
  - Traversal itératif : `GetSubtreeDepthFirst()` (pre-order), `GetSubtreeReverseDepthFirst()`
- **`UINode`** — stub vide, à développer

#### Types mathématiques
- **`Vector2<T>`** — vecteur 2D générique avec contrainte `INumber<T>`, opérateurs +, -, *, /

#### Plateforme
- **`IViewport`** — abstraction d'une surface de rendu :
  - `Size : Vector2<int>`, `FramesPerSecond`, `UpdatesPerSecond`, `VSync`
  - Signals : `OnUpdate(double delta)`, `OnRender(double delta)`, `OnResized(Vector2<int>)`, `OnClosing`
  - `Run()`, `Close()`
- **`IWindow : IViewport`** — fenêtre Desktop :
  - `Title`, `Position : Vector2<int>`, `WindowState`, `WindowBorder`, `IsVisible`, `TopMost`, `Parent`, `Monitor : IMonitor?`
  - Signals : `OnMove`, `OnStateChanged`, `OnFileDrop`
  - `CreateChildWindow(WindowOptions) : IWindow`
- **`IMonitor`** — écran physique en lecture seule : `Index`, `Name`, `IsPrimary`, `Position`, `Size`, `RefreshRate`
- **`WindowOptions`** — record de configuration (Title, Size, Position?, WindowState, WindowBorder, IsVisible, TopMost)
- **`WindowState`** — enum : Normal, Minimized, Maximized, Fullscreen
- **`WindowBorder`** — enum : Resizable, Fixed, Hidden

#### Application
- **`Application`** — static, `Run(Node root, IViewport viewport)`

### Silk (`HumbleEngine.Silk/`)
- **`SilkViewport : IViewport`** — wrapping `Silk.NET.Windowing.IView`, branche les événements Silk sur les Signals Core
- **`SilkWindow : SilkViewport, IWindow`** — wrapping `Silk.NET.Windowing.IWindow`, conversions des enums Silk ↔ Core à la frontière
- **`SilkMonitor : IMonitor`** — wrapping `Silk.NET.Windowing.IMonitor`, `IsPrimary` déterminé via `Monitor.GetMainMonitor()`

### Tests (`HumbleEngine.Tests/`)
- **`SignalTests`** (7 tests) — émission, déconnexion, ré-entrance
- **`PropertyTests`** (7 tests) — valeur initiale, ValueChanged, ReadOnly
- **`ListPropertyTests`** (13 tests) — Add, Remove, Insert, signals, AsReadOnly
- **`NodeTraversalTests`** (8 tests) — DepthFirst, ReverseDepthFirst
- **Total : 35 tests, tous verts**

---

## Décisions architecturales

### Plateforme
- Pas d'abstraction `OS` pour l'instant — on construit les abstractions Core depuis les implémentations concrètes (Silk d'abord, puis Skia, puis Mobile)
- `IViewport` = toute surface renderable (Desktop, Mobile, Web)
- `IWindow : IViewport` = fenêtre Desktop uniquement
- `IMonitor` = réalité physique en lecture seule, pas un Node — on le *découvre* via `IWindow.Monitor`, on ne le crée pas
- Parenté des fenêtres OS fixée à la création (`IWindow.CreateChildWindow()`) — reparenting non portable (Wayland l'interdit, Win32/macOS fragile)
- `WindowNode` (à faire) = Node qui possède un `IWindow` et expose ses propriétés via `Property<T>` — il HAS un IWindow, il n'en EST pas un
- Multi-fenêtre via `IWindow.CreateChildWindow()` — le Core ne sait rien des fenêtres enfants, c'est l'appli qui orchestre

### Update loop (à implémenter)
- Les Nodes qui veulent un update implémentent `IUpdate` avec `Update(double delta)`
- `UpdateFlag { Inherit, Run, DontRun }` pour contrôler l'update par sous-arbre (Inherit = héritage depuis le parent → pause d'une sous-arbre gratuite)
- Liste plate des Nodes actifs mise à jour via `OnTreeEntered`/`OnTreeExited` — O(k) par frame, pas O(n)
- Si liste vide → mode event-driven naturel, CPU soulagé
- `IUpdate` calé sur `UpdatesPerSecond` (fixed timestep, comme `_physics_process` dans Godot)
- `OnRender` calé sur `FramesPerSecond` (variable, comme `_process` dans Godot)

### Types
- `Vector2<T> where T : INumber<T>` pour distinguer pixels (int) et coordonnées logiques (float)
- Conversions Silk ↔ Core uniquement à la frontière dans `HumbleEngine.Silk`

---

## Prochaines étapes suggérées

1. **Input** — `IInput`, `IKeyboard`, `IMouse` dans Core + implémentation Silk
2. **Wiring du lifecycle** — déclencher `OnTreeEntered`/`OnTreeExited` quand le parent change
3. **`IUpdate` + UpdateFlag** — la boucle d'update sur les Nodes
4. **`WindowNode`** — Node réactif qui wraps un `IWindow`
5. **`IRenderer` (Skia)** — abstraction du rendu 2D
