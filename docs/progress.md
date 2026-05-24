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
- **`UINode : Node`** — nœud UI abstrait avec `abstract RenderElement Render()`. Le parent contrôle explicitement le rendu de ses enfants UINode dans `Render()`. Un UINode peut être enfant de n'importe quel Node — le `UIRenderPass` ne rend que les racines (UINode dont le parent n'est pas UINode). Chaque sous-arbre de UINodes = overlay indépendant.
- **`IRootNode`** — interface : `IViewport Viewport { get; }` — garantit qu'un root node possède une surface
- **`WindowNode : Node, IRootNode`** — Node Desktop, possède un `IWindow`, `Viewport => Window`
- **`Application<TRoot> where TRoot : Node, IRootNode`** — classe abstraite :
  - `Run(ApplicationConfig)` : CreateRootNode → Attach → EnterTree → OnUpdate loop → Viewport.Run → ExitTree
  - `Root` et `Config` stockés pour accès par sous-classes
  - `ConnectRenderPasses(IRenderer)` — wire les `IRenderPass` de la config sur `renderer.OnBeginFrame`
- **`ApplicationConfig`** — record : `Scene`, `WindowOptions`, `Passes : IReadOnlyList<IPass>`, `RenderPasses : IReadOnlyList<IRenderPass>`, `Default(Node)` avec UpdatePass

#### `Core/` — Système de passes
- **`IPass`** — `Execute(Node root, double delta)`, `bool ShouldExecute()` (default = true)
- **`IUpdate`** — `Update(double delta)` — interface à implémenter par les nodes qui veulent tourner chaque frame
- **`UpdatePass : IPass`** — parcourt `GetSubtreeDepthFirst().OfType<IUpdate>()`, appelle `Update(delta)`
- **`IRenderPass`** — `Execute(Node root, RenderContext context)`, `bool ShouldExecute()` (default = true)
- **`RenderContext`** — record : `IRenderer Renderer`, `ICanvas Canvas` — fourni par `renderer.OnBeginFrame`

#### `Core/` — Types partagés
- **`RawImage`** — struct : `Width`, `Height`, `Pixels : Memory<byte>` (RGBA 32-bit)

#### `Math/` — Types mathématiques
- **`Vector2<T>`** — vecteur 2D générique avec contrainte `INumber<T>`, opérateurs +, -, *, /
- **`Color`** — struct RGBA byte, constantes (Black, White, Red, etc.), `ToArgb()`, factory methods
- **`Rect`** — struct float LTRB, `FromXYWH`, `Contains`, `Inflate`, `Offset`
- **`RoundRect`** — `Rect` + `RadiusX/Y`
- **`Matrix`** — matrice 3×3 affine row-major (layout SkMatrix), `Identity`, `CreateTranslation/Scale/RotationDegrees`, `operator*`, `MapPoint`
- **`Insets`** — distances depuis les bords : `Left`, `Top`, `Right`, `Bottom` (int)

#### `Windowing/` — Plateforme fenêtrage
- **`IGraphicsContext`** — `nint GetProcAddress(string name)` — abstraction découplant Skia de Silk
- **`IViewport`** — abstraction surface de rendu, + `GraphicsContext : IGraphicsContext?`, `Input : IInputContext`
- **`IWindow : IViewport`** — fenêtre Desktop
- **`IMonitor`** — écran physique en lecture seule
- **`WindowOptions`**, **`WindowState`**, **`WindowBorder`** — types de configuration

#### `Input/` — Système d'entrées (1:1 avec `Silk.NET.Input`)
- `IInputContext`, `IKeyboard`, `IMouse`, `ICursor`, `IGamepad`, `IMotor`, `IJoystick`, `IInputDevice`
- Enums : `Key`, `MouseButton`, `CursorMode`, `ButtonName`, etc.
- Structs : `GamepadButton`, `Thumbstick`, `Trigger`, `Deadzone`, `ScrollWheel`, `Hat`

#### `Rendering/` — Interfaces de rendu 2D
- **`IRenderer`** — `Attach/Detach(IViewport)`, `OnBeginFrame : IReadOnlySignal<ICanvas>`, `OnEndFrame : IReadOnlySignal`, `CreateLinearGradient`, `CreateRadialGradient`
- **`ICanvas`** — Save/Restore, SetMatrix/Concat, Clear, DrawRect/RoundRect/Circle/Line/Path/Image/Text
- **`IPaint`** — Color, Style (Fill/Stroke/StrokeAndFill), StrokeWidth, IsAntialias, Shader
- **`IPath`** — MoveTo, LineTo, CubicTo, QuadTo, ArcTo, AddRect/RoundRect/Oval, Close, Reset
- **`IImage`**, **`ITypeface`**, **`IFont`**, **`IShader`** — interfaces minimales
- **`RenderContext`** — `IRenderer Renderer`, `ICanvas Canvas`
- **`IRenderPass`** — `Execute(Node root, RenderContext context)`

#### `UI/Elements/` — Éléments de rendu
- **`RenderElement`** (abstract record) — base de tous les éléments UI :
  - `Key : object?` — pour la réconciliation
  - `Width`, `Height`, `MinWidth`, `MaxWidth`, `MinHeight`, `MaxHeight : Length` — dimensionnement (défaut = Auto)
  - `Padding`, `Margin : EdgeInsets` — box model
  - `Opacity : float` (défaut 1f), `Background : Color`, `CornerRadius : CornerRadius`, `Overflow : Overflow`
  - `Transform : Matrix` (défaut Identity), `Anchor : Anchor?` — ignoré hors Canvas/Stack
  - Extensions fluentes génériques via `RenderElementExtensions` (préservent le type concret via `where T : RenderElement`)
- **`Box : CompositeRenderElement`** — rectangle visuel avec enfants, `BorderColor`, `BorderWidth`
- **`Text : RenderElement`** — texte réactif : `Text(string)` ou `Text(Property<string>)` — toujours backed par `Property<string>` interne (`ContentProperty : Property<string> { get; init; }`). `FontSize`, `Color`, `TextAlign`, `TextOverflow`, `ITypeface?`
- **`CompositeRenderElement : RenderElement, IEnumerable`** — base des conteneurs, `Children : ReadOnlyListProperty<RenderElement>` (backing `PrivateChildren : ListProperty<RenderElement> { get; init; }` pour préserver les enfants via `with`), `Add(RenderElement)`, `Add(IReadOnlyList<RenderElement>)`
- **`FlowLayout : CompositeRenderElement`** — layout de flux avec `Gap`, `MainAlignment`, `CrossAlignment`
- **`VLayout : FlowLayout`** — empile verticalement
- **`HLayout : FlowLayout`** — aligne horizontalement
- **`Stack : CompositeRenderElement`** — superpose (enfants positionnés par `Anchor`)

#### `UI/Types/` — Types valeur UI
- **`Length`** — struct CSS : Px, Percent, Vw, Vh, Auto (défaut, = 0 en enum), Fill. Implicit `float → Px`. Extensions sur `float`/`int` : `.Px()`, `.Percent()`, `.Vw()`, `.Vh()`
- **`EdgeInsets`** — 4 côtés float, constructeur avec défauts, `All`, `Symmetric`, implicit `float → All`
- **`CornerRadius`** — 4 coins float, constructeur avec défauts, `All`, implicit `float → All`
- **`Anchor`** — positionnement normalisé 0-1 (modèle Godot), presets : TopLeft, Center, FullStretch, etc.
- **`Overflow`** — enum : Visible (défaut), Hidden, Scroll
- **`TextAlign`** — enum : Left, Center, Right, Justify
- **`TextOverflow`** — enum : Wrap, Clip, Ellipsis, Visible
- **`MainAlignment`** — enum : Start, Center, End, SpaceBetween, SpaceAround, SpaceEvenly
- **`CrossAlignment`** — enum : Start, Center, End, Stretch

#### `UI/` — Passes UI
- **`UIRenderPass : IRenderPass`** — traverse le Node tree, rend uniquement les UINode racines (parent ≠ UINode), appelle `Render()`, passe à `Draw()` (layout engine à implémenter)

---

### Silk (`HumbleEngine.Silk/`)

#### `Windowing/`
- **`SilkApplication : Application<WindowNode>`** — `CreateRootNode` crée SilkWindow, `OnRendererCreated` appelle `ConnectRenderPasses(renderer)`
- **`SilkViewport : IViewport`** — wrapping `Silk.NET.Windowing.IView`, expose `GraphicsContext` via `SilkGraphicsContext`
- **`SilkWindow : SilkViewport, IWindow`** — wrapping `Silk.NET.Windowing.IWindow`
- **`SilkMonitor : IMonitor`** — wrapping `Silk.NET.Windowing.IMonitor`
- **`SilkGraphicsContext : IGraphicsContext`** — wrapping `IGLContext`, `GetProcAddress` via `TryGetProcAddress`

#### `Input/`
- `SilkInputContext`, `SilkKeyboard`, `SilkMouse`, `SilkCursor`, `SilkGamepad`, `SilkMotor`, `SilkJoystick`, `SilkInputDevice`

### Skia (`HumbleEngine.Skia/`)
- **`SkiaRenderer : IRenderer`** — `Attach(IViewport)`, gère `GRContext` + `SKSurface`, handle `IsInitialized`, `OnFramebufferResize`, flush sur `OnRender`. Utilise `viewport.GraphicsContext.GetProcAddress` pour `GRGlInterface`
- **`SkiaCanvas : ICanvas`**, **`SkiaPaint : IPaint`**, **`SkiaPath : IPath`**, **`SkiaImage : IImage`**, **`SkiaTypeface : ITypeface`**, **`SkiaFont : IFont`**, **`SkiaShader : IShader`** — wrappers 1:1 SkiaSharp
- Conversions internes `ToSk()` pour `Color`, `Rect`, `RoundRect`, `Matrix`
- **Découplage** : `HumbleEngine.Skia` ne référence pas `HumbleEngine.Silk` — uniquement `HumbleEngine` Core via `IGraphicsContext`

### Tests (`HumbleEngine.Tests/`)
- **`SignalTests`** (7 tests), **`PropertyTests`** (7 tests), **`ListPropertyTests`** (13 tests)
- **`NodeTraversalTests`** (8 tests), **`NodeLifecycleTests`** (8 tests)
- **Total : 43 tests, tous verts**

---

## Décisions architecturales

### UI System
- `RenderElement` = record abstrait (égalité structurelle + expressions `with` pour API fluente immuable)
- Fluent API via extension methods génériques `where T : RenderElement` — préservent le type concret
- `Length.Auto = 0` en premier dans l'enum → `default(Length)` = Auto sans écriture explicite
- `EdgeInsets`/`CornerRadius` : implicit `float` → uniform — `.Padding(12)` valide sans surcharge
- `CompositeRenderElement` utilise `private ListProperty<RenderElement> PrivateChildren { get; init; }` — `init` permet à `with` de copier la référence ; `Add` fonctionne pendant l'initializer ; exposition via `ReadOnlyListProperty`
- `Text` toujours backed par `Property<string>` — string statique wrappée, évite deux chemins dans le renderer
- `Box` étend `CompositeRenderElement` — un div peut avoir des enfants
- `UINode.Render()` explicite : le parent contrôle quels enfants apparaissent, quand, combien de fois
- UINode peut être n'importe où dans le Node tree. `UIRenderPass` ne rend que les racines (parent ≠ UINode) → chaque sous-arbre UINode = overlay indépendant
- Animations : architecture identifiée (IAnimation<T> : IReadOnlyProperty<T>, ImplicitAnimation, TransitionAnimation, Tween, AnimationPass) mais différée après layout engine

### Organisation des fichiers
- `UI/Elements/` — éléments et leurs extensions dans le même fichier
- `UI/Types/` — structs et enums valeur
- `namespace HumbleEngine` unique pour tout le Core

### Rendu
- `IGraphicsContext` dans Core → Skia découplé de Silk
- `IRenderPass` / `RenderContext` — passes de rendu branchées sur `renderer.OnBeginFrame` via `Application.ConnectRenderPasses`
- `Application` stocke `Root` et `Config` pour que `ConnectRenderPasses` soit callable depuis `SilkApplication.OnRendererCreated`

---

## Prochaines étapes

1. **Layout engine** — deux passes : Measure (taille souhaitée) puis Layout (position finale) sur le RenderElement tree
2. **`UIRenderPass.Draw()`** — dessiner un `RenderElement` via `ICanvas` après layout
3. **Réconciliation** — diff entre ancienne et nouvelle RenderElement tree, mise à jour minimale
4. **Animations** — `IAnimation<T> : IReadOnlyProperty<T>`, `Tween<T>`, `ImplicitAnimation<T>`, `TransitionAnimation<T>`, `AnimationPass`
5. **Propriétés animables** — refactoring `RenderElement` pour que les champs acceptent `IReadOnlyProperty<T>` (statique ou animé)
6. **Input sur UINode** — `OnMouseEnter`, `OnMouseExit`, `OnClick`, hit-testing
7. **`IRenderer` OpenGL** — implémentation directe pour comparaison avec Skia
8. **`UpdateFlag`** — contrôle update par sous-arbre
