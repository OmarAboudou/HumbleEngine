# HumbleEngine — Progress

## Ce qui est implémenté

### Core (`HumbleEngine/`)

#### `Core/` — Système réactif
- **`Signal`**, **`Signal<T>`**, **`Signal<T1,T2>`** — système d'événements avec Connect/Disconnect/Emit
- **`ReadOnlySignal`** — vue en lecture seule d'un signal (Connect/Disconnect seulement)
- **`IReadOnlySignal`** — interfaces correspondantes
- **`Property<T>`** — propriété réactive avec `ValueChanged : IReadOnlySignal<T,T>` (old, new)
- **`ReadOnlyProperty<T>`** — vue en lecture seule
- **`ListProperty<T>`** — liste réactive avec signaux `Added` et `Removed`
- **`ReadOnlyListProperty<T>`** — vue en lecture seule avec accès aux signaux
- Fix : `Signal.Emit` itère sur un snapshot pour éviter InvalidOperationException si un callback modifie les connexions pendant l'émission

#### `Core/` — Arbre de scène
- **`Node`** — nœud abstrait avec :
  - Relation parent/enfant via `Property<Node?> _parent` et `ListProperty<Node> _children`
  - `SetParent()`, `Attach()`, `Detach()`
  - Lifecycle wirés : `OnTreeEntered` (top-down), `OnChildrenEntered` (bottom-up), `OnChildrenExited` (top-down), `OnTreeExited` (bottom-up)
  - `internal bool _isInTree` — état d'appartenance à l'arbre actif
  - Traversal itératif : `GetSubtreeDepthFirst()` (pre-order), `GetSubtreeReverseDepthFirst()`
- **`UINode`** — stub vide, à développer
- **`IRootNode`** — interface : `IViewport Viewport { get; }` — garantit qu'un root node possède une surface
- **`WindowNode : Node, IRootNode`** — Node Desktop, possède un `IWindow`, `Viewport => Window`
- **`Application<TRoot> where TRoot : Node, IRootNode`** — classe abstraite, `Run(ApplicationConfig)` :
  - Appelle `CreateRootNode(config)` (abstract)
  - Attache `config.Scene` au root
  - Bootstrap `EnterTree` / `ExitTree`
  - Souscrit `OnUpdate` → exécute les passes de `config.Passes`
  - Appelle `root.Viewport.Run()` (pas de RunLoop() séparé)
- **`ApplicationConfig`** — record : `Scene : Node`, `WindowOptions`, `Passes : IReadOnlyList<IPass>` (init, défaut vide), `Default(Node)` avec UpdatePass

#### `Core/` — Système de passes
- **`IPass`** — `Execute(Node root, double delta)`, `bool ShouldExecute()` (default impl = true)
- **`IUpdate`** — `Update(double delta)` — interface à implémenter par les nodes qui veulent tourner chaque frame
- **`UpdatePass : IPass`** — parcourt `GetSubtreeDepthFirst().OfType<IUpdate>()`, appelle `Update(delta)`
- **Note architecture** : les passes sont pilotées par `Application` (pas auto-exécutantes). Toutes les passes actuelles tournent sur `OnUpdate`. Quand les passes render arriveront, un mécanisme de trigger par signal sera ajouté (ex: `OnRender` pour UIRenderPass)

#### `Core/` — Types partagés
- **`RawImage`** — struct : `Width`, `Height`, `Pixels : Memory<byte>` (RGBA 32-bit)

#### `Math/` — Types mathématiques
- **`Vector2<T>`** — vecteur 2D générique avec contrainte `INumber<T>`, opérateurs +, -, *, /
- **`Insets`** — distances depuis les bords : `Left`, `Top`, `Right`, `Bottom` (int)

#### `Windowing/` — Plateforme fenêtrage
- **`IViewport`** — abstraction d'une surface de rendu (1:1 avec `Silk.NET.Windowing.IView`) :
  - `Handle`, `IsInitialized`, `IsClosing`, `Time`, `Size`, `FramebufferSize`
  - `FramesPerSecond`, `UpdatesPerSecond`, `VSync`
  - Signals : `OnLoad`, `OnUpdate(double)`, `OnRender(double)`, `OnResized`, `OnFramebufferResize`, `OnFocusChanged(bool)`, `OnClosing`
  - `Input : IInputContext`
  - `Focus()`, `Run()`, `Close()`
  - `PointToClient()`, `PointToScreen()`, `PointToFramebuffer()`
- **`IWindow : IViewport`** — fenêtre Desktop (1:1 avec `Silk.NET.Windowing.IWindow`) :
  - `Title`, `Position`, `WindowState`, `WindowBorder`, `IsVisible`, `TopMost`
  - `Parent : IWindow?`, `Monitor : IMonitor?`, `BorderSize : Insets`
  - Signals : `OnMove`, `OnStateChanged`, `OnFileDrop`
  - `CreateChildWindow(WindowOptions) : IWindow`
  - `SetWindowIcon(ReadOnlySpan<RawImage>)`
- **`IMonitor`** — écran physique en lecture seule : `Index`, `Name`, `IsPrimary`, `Position`, `Size`, `RefreshRate`
- **`WindowOptions`** — record de configuration (Title, Size, Position?, WindowState, WindowBorder, IsVisible, TopMost)
- **`WindowState`** — enum : Normal, Minimized, Maximized, Fullscreen
- **`WindowBorder`** — enum : Resizable, Fixed, Hidden

#### `Input/` — Système d'entrées (1:1 avec `Silk.NET.Input`)
- **`IInputContext`** — point d'entrée : `Handle`, `Keyboards`, `Mice`, `Gamepads`, `Joysticks`, `OtherDevices`, `OnConnectionChanged`
- **`IKeyboard`** — `SupportedKeys`, `ClipboardText`, `IsKeyPressed(Key)`, `IsScancodePressed(int)`, `BeginInput()`, `EndInput()`, signals `OnKeyDown(Key, int)`, `OnKeyUp(Key, int)`, `OnKeyChar(char)`
- **`IMouse`** — `SupportedButtons`, `ScrollWheels`, `Position`, `Cursor`, `DoubleClickTime`, `DoubleClickRange`, `IsButtonPressed(MouseButton)`, signals `OnButtonDown`, `OnButtonUp`, `OnClick`, `OnDoubleClick`, `OnMove`, `OnScroll`
- **`ICursor`** — `Type`, `StandardCursor`, `CursorMode`, `IsConfined`, `HotspotX`, `HotspotY`, `Image : RawImage`, `IsSupported(CursorMode)`, `IsSupported(StandardCursor)`
- **`IInputDevice`** — `Name`, `Index`, `IsConnected`
- **`IGamepad : IInputDevice`** — `Buttons`, `Thumbsticks`, `Triggers`, `VibrationMotors`, `Deadzone`, signals `OnButtonDown`, `OnButtonUp`, `OnThumbstickMoved`, `OnTriggerMoved`
- **`IMotor`** — `Index`, `Speed` (get/set)
- **`ButtonName`** — enum : Unknown, A, B, X, Y, LeftBumper, RightBumper, Back, Start, Home, LeftStick, RightStick, DPadUp, DPadRight, DPadDown, DPadLeft
- **`GamepadButton`** — struct readonly : `Name : ButtonName`, `Index`, `Pressed`
- **`Thumbstick`** — struct readonly : `Index`, `X`, `Y`, `Position` (magnitude), `Direction` (atan2)
- **`Trigger`** — struct readonly : `Index`, `Position`
- **`DeadzoneMethod`** — enum : Traditional, AdaptiveGradient
- **`Deadzone`** — struct readonly : `Value`, `Method`, `Apply(float)`
- **`IJoystick : IInputDevice`** — `Axes`, `Buttons`, `Hats`, `Deadzone`, signals `OnButtonDown`, `OnButtonUp`, `OnAxisMoved`, `OnHatMoved`
- **`Axis`** — struct readonly : `Index`, `Position`
- **`HatPosition`** — enum : Centered, Up, Down, Left, Right, UpLeft, UpRight, DownLeft, DownRight
- **`Hat`** — struct readonly : `Index`, `Position : HatPosition`
- **Enums** : `Key`, `MouseButton`, `CursorMode`, `CursorType`, `StandardCursor`
- **Struct** : `ScrollWheel` — `X : float`, `Y : float`

### Silk (`HumbleEngine.Silk/`)

#### `Windowing/`
- **`SilkApplication : Application<WindowNode>`** — `CreateRootNode` crée `SilkWindow.Create(opts)` → `WindowNode`, `Viewport.Run()` démarre la boucle (pas de RunLoop séparé)
- **`SilkViewport : IViewport`** — wrapping `Silk.NET.Windowing.IView`
- **`SilkWindow : SilkViewport, IWindow`** — wrapping `Silk.NET.Windowing.IWindow`, conversions enums Silk ↔ Core à la frontière
- **`SilkMonitor : IMonitor`** — wrapping `Silk.NET.Windowing.IMonitor`, `IsPrimary` via `Monitor.GetMainMonitor()`

#### `Input/`
- **`SilkInputContext : IInputContext`** — listes live (pas snapshot), wrappers cachés par instance Silk, purge au disconnect
- **`SilkKeyboard : IKeyboard`** — wrapping `Silk.NET.Input.IKeyboard`
- **`SilkMouse : IMouse`** — wrapping `Silk.NET.Input.IMouse`
- **`SilkCursor : ICursor`** — wrapping `Silk.NET.Input.ICursor`
- **`SilkInputDevice : IInputDevice`** — wrapping `Silk.NET.Input.IInputDevice`
- **`SilkGamepad : IGamepad`** — wrapping `Silk.NET.Input.IGamepad`, conversions ButtonName/DeadzoneMethod à la frontière
- **`SilkMotor : IMotor`** — wrapping `Silk.NET.Input.IMotor`
- **`SilkJoystick : IJoystick`** — wrapping `Silk.NET.Input.IJoystick`, conversion `Position2D` → `HatPosition` à la frontière

### Tests (`HumbleEngine.Tests/`)
- **`SignalTests`** (7 tests) — émission, déconnexion, ré-entrance
- **`PropertyTests`** (7 tests) — valeur initiale, ValueChanged, ReadOnly
- **`ListPropertyTests`** (13 tests) — Add, Remove, Insert, signals, AsReadOnly
- **`NodeTraversalTests`** (8 tests) — DepthFirst, ReverseDepthFirst
- **`NodeLifecycleTests`** (8 tests) — EnterTree/ExitTree, propagation, reparenting, hors arbre
- **Total : 43 tests, tous verts**

---

## Décisions architecturales

### Organisation des fichiers
- Dossiers miroirs des packages Silk : `Core/`, `Math/`, `Windowing/`, `Input/`
- `namespace HumbleEngine` unique pour tout le Core (pas de sous-namespaces)
- `namespace HumbleEngine.Silk` unique pour tout Silk
- Même structure de dossiers dans `HumbleEngine.Silk/`

### Stratégie d'abstraction
- Mapping 1:1 avec la lib sous-jacente tant qu'une seule plateforme est supportée
- Les compromis et réconciliations se feront quand une 2e plateforme arrivera
- Exception : détails internes non exposés dans le Core

### Plateforme
- `IViewport` = toute surface renderable (Desktop, Mobile, Web)
- `IWindow : IViewport` = fenêtre Desktop uniquement
- `IMonitor` = réalité physique en lecture seule — on le *découvre* via `IWindow.Monitor`, on ne le crée pas
- `WindowNode : Node, IRootNode` — HAS-A `IWindow`, pas IS-A. `Viewport => Window`
- `IRootNode` — garantit compile-time qu'un root node a un `IViewport`. `Application<TRoot>` contraint `TRoot : Node, IRootNode`
- Multi-fenêtre via `IWindow.CreateChildWindow()` — le Core ne sait rien des fenêtres enfants

### Application et passes
- `Application<TRoot>` est abstraite — `SilkApplication` implémente `CreateRootNode`
- `ApplicationConfig` : `Scene` (arbre utilisateur), `WindowOptions`, `Passes`
- `CreateRootNode` crée le root platform-specific (ex: `WindowNode` sur Desktop) et l'attach à la boucle
- Les passes sont passives — pilotées par `Application` via `OnUpdate`, pas auto-exécutantes
- Toutes les passes actuelles sur `OnUpdate`. Quand render passes arrivent : ajout d'un `Trigger` sur `IPass` pour brancher sur `OnRender`
- `ShouldExecute()` — default impl sur `IPass`, permet à une passe de se désactiver (ex: UIRenderPass si rien n'a changé)

### Types
- `Vector2<T> where T : INumber<T>` pour distinguer pixels (int) et coordonnées logiques (float)
- `Insets` (pas `Thickness`) — terme issu d'Android/iOS/Flutter, décrit des distances vers l'intérieur
- `RawImage` — pixels RGBA 32-bit non-prémultipliés, little-endian

### Rendu 2D (en cours)
- Deux implémentations prévues pour comparaison : `HumbleEngine.Skia` (SkiaSharp) et `HumbleEngine.OpenGL` (OpenGL direct)
- `HumbleEngine.Skia` créé, SkiaSharp à ajouter
- Skia = backend de dessin uniquement (pas de layout, pas de widgets, pas d'events)
- Core définira : `IRenderer`, `ICanvas`, `IPaint`, `IPath`, `IImage`, `ITypeface`, `IFont`, `IShader`
- Types valeur Core : `Color`, `Rect`, `RoundRect`, `Matrix`
- `IRenderer` = point d'entrée : fournit `ICanvas` chaque frame, gère `GRContext` + `SKSurface` en interne
- Mapping 1:1 SkiaSharp → Core, réconciliation OpenGL plus tard
- `SkiaRenderer` se branche sur `IViewport.OnLoad`, `OnFramebufferResize`, flushe sur `OnRender`

---

## Prochaines étapes

1. **Rendu Skia** — valeur types (Color, Rect, RoundRect, Matrix), puis IRenderer → ICanvas → IPaint → IPath → IImage → IFont/ITypeface → IShader
2. **`UpdateFlag`** — contrôle update par sous-arbre (Inherit, Run, DontRun)
3. **Liste plate `UpdatePass`** — O(k) via OnTreeEntered/OnTreeExited au lieu de O(n) traversal
4. **Pass trigger** — `IPass.Trigger` pour brancher certaines passes sur `OnRender` plutôt que `OnUpdate`
5. **`UINode`** — développer le nœud UI de base
6. **`IRenderer` OpenGL** — implémentation directe pour comparaison avec Skia
