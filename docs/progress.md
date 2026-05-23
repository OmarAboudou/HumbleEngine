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
  - Lifecycle : `OnTreeEntered`, `OnTreeExited`, `OnChildrenEntered`, `OnChildrenExited` (définis, pas encore wirés)
  - Traversal itératif : `GetSubtreeDepthFirst()` (pre-order), `GetSubtreeReverseDepthFirst()`
- **`UINode`** — stub vide, à développer
- **`Application`** — static, `Run(Node root, IViewport viewport)`

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
- **`SilkViewport : IViewport`** — wrapping `Silk.NET.Windowing.IView`
- **`SilkWindow : SilkViewport, IWindow`** — wrapping `Silk.NET.Windowing.IWindow`, conversions enums Silk ↔ Core à la frontière
- **`SilkMonitor : IMonitor`** — wrapping `Silk.NET.Windowing.IMonitor`, `IsPrimary` via `Monitor.GetMainMonitor()`

#### `Input/`
- **`SilkInputContext : IInputContext`** — wrapping `Silk.NET.Input.IInputContext`
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
- **Total : 35 tests, tous verts**

---

## Décisions architecturales

### Organisation des fichiers
- Dossiers miroirs des packages Silk : `Core/`, `Math/`, `Windowing/`, `Input/`
- `namespace HumbleEngine` unique pour tout le Core (pas de sous-namespaces)
- `namespace HumbleEngine.Silk` unique pour tout Silk
- Même structure de dossiers dans `HumbleEngine.Silk/`

### Stratégie d'abstraction
- Mapping 1:1 avec Silk tant qu'aucune autre plateforme n'est supportée
- Les compromis et réconciliations se feront quand une 2e plateforme arrivera (SDL, Mobile...)
- Exception : détails internes de la boucle Silk (`DoRender`, `DoUpdate`, `DoEvents`, etc.) non exposés dans le Core

### Plateforme
- `IViewport` = toute surface renderable (Desktop, Mobile, Web)
- `IWindow : IViewport` = fenêtre Desktop uniquement
- `IMonitor` = réalité physique en lecture seule — on le *découvre* via `IWindow.Monitor`, on ne le crée pas
- Parenté des fenêtres OS fixée à la création (`CreateChildWindow()`) — reparenting non portable (Wayland l'interdit)
- `WindowNode` (à faire) = Node qui possède un `IWindow` — il HAS un IWindow, il n'en EST pas un
- Multi-fenêtre via `IWindow.CreateChildWindow()` — le Core ne sait rien des fenêtres enfants

### Types
- `Vector2<T> where T : INumber<T>` pour distinguer pixels (int) et coordonnées logiques (float)
- `Insets` (pas `Thickness`) — terme issu d'Android/iOS/Flutter, décrit des distances vers l'intérieur
- `RawImage` — pixels RGBA 32-bit non-prémultipliés, little-endian (aligné sur `Silk.NET.Core.RawImage`)
- Conversions Silk ↔ Core uniquement à la frontière dans `HumbleEngine.Silk`

### Update loop (à implémenter)
- Les Nodes qui veulent un update implémentent `IUpdate` avec `Update(double delta)`
- `UpdateFlag { Inherit, Run, DontRun }` pour contrôler l'update par sous-arbre
- Liste plate des Nodes actifs mise à jour via `OnTreeEntered`/`OnTreeExited` — O(k) par frame
- `IUpdate` calé sur `UpdatesPerSecond` (fixed timestep)
- `OnRender` calé sur `FramesPerSecond` (variable)

---

## Prochaines étapes suggérées

1. **Wiring du lifecycle** — déclencher `OnTreeEntered`/`OnTreeExited` quand le parent change
2. **Wiring du lifecycle** — déclencher `OnTreeEntered`/`OnTreeExited` quand le parent change
3. **`IUpdate` + UpdateFlag** — la boucle d'update sur les Nodes
4. **`WindowNode`** — Node réactif qui wraps un `IWindow`
5. **`IRenderer` (Skia)** — abstraction du rendu 2D
