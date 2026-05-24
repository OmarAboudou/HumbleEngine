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
  - Lifecycle : `OnTreeEntered` (top-down), `OnChildrenEntered` (bottom-up), `OnChildrenExited` (top-down), `OnTreeExited` (bottom-up)
  - `internal bool _isInTree` — état d'appartenance à l'arbre actif
  - Traversal itératif : `GetSubtreeDepthFirst()` (pre-order), `GetSubtreeReverseDepthFirst()`
- **`UINode : Node`** — nœud UI abstrait avec `abstract RenderElement Render()`. `UIRenderPass` ne rend que les racines (UINode dont le parent n'est pas UINode). Chaque sous-arbre = overlay indépendant. `internal bool IsDirty` expose le flag dirty pour le caching côté moteur.
- **`IRootNode`** — interface : `IViewport Viewport { get; }`
- **`WindowNode : Node, IRootNode`** — Node Desktop, possède un `IWindow`
- **`Application<TRoot> where TRoot : Node, IRootNode`** — classe abstraite :
  - `Run(ApplicationConfig)` : CreateRootNode → Attach → EnterTree → passes → Viewport.Run → ExitTree
  - Deux méthodes abstraites : `CreateRootNode(ApplicationConfig)` et `CreateRenderer(GraphicsAPI)`
  - Lifecycle renderer entièrement géré : création dans `OnLoad`, rendu via `renderer.OnBeginFrame`, détachement dans `OnClosing`
  - `BlackBoard` partagé entre toutes les passes (update + render) sur toute la durée de l'application
  - Dispatch pré-calculé : `IUpdatePass[]` et `IRenderPass[]` extraits une seule fois au `Run`
- **`ApplicationConfig`** — record : `Scene`, `WindowOptions`, `Api : GraphicsAPI` (défaut OpenGL), `Passes : IReadOnlyList<IPass>`. `Default(Node)` inclut `UpdatePass`.

#### `Core/` — Système de passes
- **`IPass`** — marqueur avec `bool ShouldExecute()` (default = true) ; liste unique dans `ApplicationConfig`
- **`IUpdatePass : IPass`** — `Execute(Node root, double delta, BlackBoard board)`
- **`IRenderPass : IPass`** — `Execute(Node root, RenderContext context, BlackBoard board)`
- Une passe peut implémenter les deux interfaces et s'exécuter sur les deux signaux
- **`UpdatePass : IUpdatePass`** — parcourt `GetSubtreeDepthFirst().OfType<IUpdate>()`, appelle `Update(delta)`
- **`IUpdate`** — `Update(double delta)` — interface à implémenter par les nodes qui veulent tourner chaque frame
- **`BlackBoard`** — données partagées entre passes, type-keyed : `Set<T>(value)` / `Get<T>()`. Aucune collision possible entre passes engine et passes utilisateur.
- **`GraphicsAPI`** — enum : `OpenGL`, `Vulkan`, `Metal`, `WebGL`, `Software`
- **`RenderContext`** — record : `IRenderer Renderer`, `ICanvas Canvas`

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
- **`IViewport`** — abstraction surface de rendu : signaux `OnLoad`, `OnUpdate`, `OnRender`, `OnFramebufferResize`, `OnResized`, `OnFocusChanged`, `OnClosing` ; `Input : IInputContext`
- **`IWindow : IViewport`** — fenêtre Desktop
- **`IMonitor`** — écran physique en lecture seule
- **`WindowOptions`**, **`WindowState`**, **`WindowBorder`** — types de configuration

#### `Input/` — Système d'entrées (1:1 avec `Silk.NET.Input`)
- `IInputContext`, `IKeyboard`, `IMouse`, `ICursor`, `IGamepad`, `IMotor`, `IJoystick`, `IInputDevice`
- Enums : `Key`, `MouseButton`, `CursorMode`, `ButtonName`, etc.
- Structs : `GamepadButton`, `Thumbstick`, `Trigger`, `Deadzone`, `ScrollWheel`, `Hat`

#### `Rendering/` — Interfaces et abstractions de rendu
- **`IRenderer`** — contrat consommateurs : `Attach/Detach(IViewport)`, `OnBeginFrame : IReadOnlySignal<ICanvas>`, `OnEndFrame`, `CreatePaint()`, `CreateLinearGradient`, `CreateRadialGradient`
- **`RendererBase : IRenderer`** — classe abstraite Core : lifecycle complet (signaux, Attach/Detach, OnFramebufferResize, OnRender). Points d'extension abstraits : `Initialize()`, `CreateSurface()`, `DestroySurface()`, `GetCanvas()`, `Flush()` + virtuel `DestroyGraphics()`
- **`ICanvas : ITextMeasurer`** — Save/Restore, transforms, Clear, Draw* ; `MeasureText(string, Font)` hérité de `ITextMeasurer` ; `DrawText(string, float, float, Font, IPaint)`
- **`ITextMeasurer`** — `float MeasureText(string text, Font font)` + extension `string[] BreakLines(string, Font, float maxWidth)` (split `\n` + word-wrap glouton)
- **`Font`** — readonly struct : `Typeface?`, `Size`, `ScaleX`, `SkewX`. `Font.Default = new Font(16f)`. Constructeurs `Font(float size)` et `Font(Typeface, float size)`.
- **`Typeface`** — readonly struct : `FamilyName`, `IsBold`, `IsItalic`. `null` = police système par défaut.
- **`IPaint`** — Color, Style (Fill/Stroke/StrokeAndFill), StrokeWidth, IsAntialias, Shader
- **`IPath`** — MoveTo, LineTo, CubicTo, QuadTo, ArcTo, AddRect/RoundRect/Oval, Close, Reset
- **`IImage`**, **`IShader`** — interfaces minimales
- **`IRenderPass : IPass`** — `Execute(Node root, RenderContext context, BlackBoard board)`

#### `UI/Elements/` — Éléments de rendu
- **`RenderElement`** (abstract record) — base de tous les éléments UI :
  - `Width`, `Height`, `MinWidth`, `MaxWidth`, `MinHeight`, `MaxHeight : Length` — dimensionnement (défaut = Auto)
  - `Padding`, `Margin : EdgeInsets` — box model
  - `Opacity : float`, `Background : Color`, `BorderColor : Color`, `BorderWidth : float`, `CornerRadius : CornerRadius`
  - `Transform : Matrix` (défaut Identity), `Anchor : Anchor?`
  - Events : `MouseEnter`, `MouseExit`, `Click : Action?`
  - Extensions fluentes génériques via `RenderElementExtensions` : fluent setters + `OnMouseEnter/Exit/Click`, `When(bool, Func<T,T>, Func<T,T>?)`
- **`NodeElement(UINode Node) : RenderElement`** — couture de composition : embarque un `UINode` dans un arbre `RenderElement`. Le `LayoutEngine` résout `Node.GetElement()` à chaque frame (cache interne du UINode évite le recalcul si non-dirty). Permet à un UINode d'être enfant de layout d'un autre UINode.
- **`Text : RenderElement`** — texte une ligne (`<span>`) : `Content`, `Font`, `Color`, `TextAlign`, `TextOverflow`. Taille intrinsèque : largeur = `MeasureText(content, font)`, hauteur = `font.Size`. Backed par `Property<string>` interne.
- **`TextBlock : RenderElement`** — texte multi-ligne (`<p>`) : mêmes propriétés que `Text`. Largeur = espace disponible (remplit le conteneur). Hauteur = `BreakLines(content, font, width).Length × font.Size`. **Requiert un parent à largeur contrainte** (Px, Fill, ou Percent) — dans un parent fit-content, TextBlock empêche le rétrécissement.
- **`CompositeRenderElement : RenderElement, IEnumerable`** — base des conteneurs. `Add(RenderElement)`, `Add(UINode)` (→ NodeElement implicite), `Add(Func<RenderElement>)` (method group), `Add(IEnumerable<RenderElement>)` (LINQ)
- **`FlowLayout : CompositeRenderElement`** — layout de flux avec `Gap`, `MainAlignment`, `CrossAlignment`
- **`VLayout : FlowLayout`** — empile verticalement
- **`HLayout : FlowLayout`** — aligne horizontalement
- **`Stack : CompositeRenderElement`** — superpose (enfants positionnés par `Anchor`)

#### `UI/Types/` — Types valeur UI
- **`Length`** — struct CSS : Px, Percent, Vw, Vh, Auto (défaut), Fill. Implicit `float → Px`
- **`EdgeInsets`** — 4 côtés float, `All`, `Symmetric`, implicit `float → All`
- **`CornerRadius`** — 4 coins float, `All`, implicit `float → All`
- **`Anchor`** — positionnement normalisé 0-1, presets : TopLeft, Center, FullStretch, etc.
- **`Overflow`**, **`TextAlign`**, **`TextOverflow`**, **`MainAlignment`**, **`CrossAlignment`** — enums

#### `UI/Layout/` — Layout engine
- **`LayoutEngine`** — calcule positions et dimensions du RenderElement tree :
  - Requis : `ITextMeasurer` (non-nullable) — utilisé pour tailles intrinsèques de `Text` et `TextBlock`
  - `Layout(UINode node, float vw, float vh, ITextMeasurer)` → `LayoutNode`
  - `Compute` : résout `NodeElement` avant dispatch (`while (el is NodeElement ne) el = ne.Node.GetElement()`) ; helper `Effective(el)` utilisé pour les décisions de layout (Fill, Anchor, marges) avant résolution complète
  - `ComputeLeaf` : `Text` → largeur intrinsèque via `MeasureText`, hauteur = `font.Size` ; `TextBlock` → largeur = `availW`, hauteur = `BreakLines(...).Length × font.Size`
  - `ComputeFlow` (VLayout/HLayout) : 3 passes — mesure non-Fill, distribue Fill, positionne
  - `ComputeStack` — enfants positionnés par `Anchor`
  - `ApplyMinMax` — clamp Width/Height entre MinWidth/MaxWidth
- **`LayoutNode`** — record : `RenderElement Element`, `LayoutBox Box`, `IReadOnlyList<LayoutNode> Children`, `ElementId Id`
- **`LayoutBox`** — struct : `X`, `Y`, `Width`, `Height`, `ToRect()`, `Contains(Vector2<float>)`
- **`Constraints`** — `MaxWidth`, `MaxHeight` offerts par le parent

#### `UI/` — Passes UI
- **`UIRenderPass : IRenderPass`** — layout + rendu complet :
  - Passe `ICanvas` (qui est `ITextMeasurer`) au `LayoutEngine`
  - `DrawBackground`, `DrawBorder`, `DrawText` (single-line avec alignement), `DrawTextBlock` (multi-ligne via `BreakLines`)
  - Transforms : Save/Restore canvas si `Transform ≠ Identity`
  - **Layout caching** : cache `UINode → (LayoutNode, vw, vh)` ; ne recompute que si le viewport change ou si `anyDirty` (`GetSubtreeDepthFirst().OfType<UINode>().Any(n => n.IsDirty)`)
- **`InputPass : IUpdatePass`** — hit-testing sur `UILayoutCache`, dispatch events :
  - Détecte entrée/sortie hover par comparaison frame courante vs frame précédente → fire `MouseEnter`/`MouseExit`
  - Détecte clic (relâchement bouton gauche) → fire `Click`
  - Collecte récursive sur le LayoutNode tree : remonte parents et enfants sous le curseur

---

### Silk (`HumbleEngine.Silk/`)

#### `Windowing/`
- **`SilkApplication : Application<WindowNode>`** — override `CreateRootNode` (crée `SilkWindow`) ; `CreateRenderer(GraphicsAPI)` laissé abstrait pour la classe applicative
- **`SilkViewport : IViewport`** — wrapping `Silk.NET.Windowing.IView`
- **`SilkWindow : SilkViewport, IWindow`** — wrapping `Silk.NET.Windowing.IWindow`
- **`SilkMonitor : IMonitor`** — wrapping `Silk.NET.Windowing.IMonitor`

#### `Input/`
- `SilkInputContext`, `SilkKeyboard`, `SilkMouse`, `SilkCursor`, `SilkGamepad`, `SilkMotor`, `SilkJoystick`, `SilkInputDevice`

---

### Skia (`HumbleEngine.Skia/`)
- **`SkiaRenderer : RendererBase`** — implémente `Initialize` (GRContext GL), `CreateSurface` (SKSurface), `DestroySurface`, `GetCanvas`, `Flush`, `DestroyGraphics` (GRContext.Dispose). `CreatePaint`, `CreateLinearGradient`, `CreateRadialGradient`.
- **`SkiaCanvas : ICanvas`** — `DrawText(Font)`, `MeasureText(string, Font)` : convertit `Font` → `SKFont` en interne à chaque appel. `ToSk(Font)` : résout la `Typeface` via `SKTypeface.FromFamilyName` ou `SKTypeface.Default`.
- **`SkiaPaint : IPaint`**, **`SkiaPath : IPath`**, **`SkiaImage : IImage`**, **`SkiaShader : IShader`** — wrappers 1:1 SkiaSharp
- Conversions internes `ToSk()` pour `Color`, `Rect`, `RoundRect`, `Matrix`, `Font`

---

### Demo (`HumbleEngine.Demo/`)
- **`DemoApp : SilkApplication`** — override `CreateRenderer(GraphicsAPI)` → `new SkiaRenderer()`
- **`DashboardNode : UINode, IUpdate`** — dashboard complet : Header, Sidebar, Content (deux sections), Footer. Démontre `VLayout`, `HLayout`, `Stack`, `Text`, `TextBlock`, `NodeElement`, `When`, `OnMouseEnter/Exit/Click`.
- **`NavItemNode : UINode`** — item de navigation avec état hover isolé (`_hovered`), embarqué via `NodeElement` dans le layout de `DashboardNode`. Démontre la composition UINode-dans-UINode.
- Section "Events" dans le Content : hover border, hover background, click counter.

---

### Tests (`HumbleEngine.Tests/`)
- **`SignalTests`** (7 tests), **`PropertyTests`** (7 tests), **`ListPropertyTests`** (13 tests)
- **`NodeTraversalTests`** (8 tests), **`NodeLifecycleTests`** (8 tests)
- **`LayoutEngineTests`** (28 tests) — Leaf, VLayout, HLayout, Stack : dimensions, Fill, Auto, Gap, Padding, Margin, alignements. Helper `TestNode : UINode` + `NoOpMeasurer` pour tester sans canvas.
- **`AnimationTests`** (18 tests) — `Tween<T>` : valeur initiale, `To()`, avance, completion, idempotence, interruption, dépassement. `Easing` : bornes Linear/EaseOut. `Lerp` : float, Color. `ImplicitAnimation<T>` : état initial, changement propriété, atteinte cible, interruption. `UINode.AdvanceAnimations` : dirty quand en cours, pas dirty quand complet.
- **Total : 89 tests, tous verts**

---

## Décisions architecturales

### Système de passes
- `IPass` = marqueur unique — une seule liste dans `ApplicationConfig`, plus de séparation `Passes`/`RenderPasses`
- `IUpdatePass` et `IRenderPass` dérivent de `IPass` — une même classe peut implémenter les deux
- `BlackBoard` type-keyed : chaque passe publie ses données sous son propre type, aucune collision, extensible par les utilisateurs du moteur
- Dispatch pré-calculé au `Run` : `OfType<IUpdatePass>().ToArray()` une fois, pas à chaque frame

### Renderer
- `IRenderer` = contrat consommateurs (Application, passes, RenderContext) — dépendance sur l'interface, pas sur la classe
- `RendererBase` = aide implémenteurs (template method) — lifecycle, signaux, Attach/Detach centralisés
- `Application` internalise `CreateRenderer` + lifecycle : `SilkApplication` réduit à la création de fenêtre ; `DemoApp` n'override qu'une méthode

### Types de police
- `IFont`/`ITypeface` (interfaces + objets natifs) → `Font`/`Typeface` (readonly structs) : pas d'allocation, pas de `IDisposable`, instanciables n'importe où dans Core
- `SkiaCanvas` convertit `Font` → `SKFont` en interne à chaque usage (appel Skia temporaire)
- `MeasureText` sur `ICanvas` (= `ITextMeasurer`) : mesurer = opération plateforme, pas une méthode sur la donnée

### Layout et texte
- `Text` = inline (`<span>`) : taille intrinsèque, une ligne, pas de wrap
- `TextBlock` = bloc (`<p>`) : remplit la largeur disponible, wrap automatique sur `\n` et mots, hauteur calculée ; **requiert un parent à largeur contrainte**
- `LayoutEngine` requiert `ITextMeasurer` non-nullable — l'oubli est une erreur de compilation
- `UIRenderPass` passe `context.Canvas` comme `ITextMeasurer` au layout (le canvas implémente les deux)

### UI System
- `RenderElement` = record abstrait immuable — égalité structurelle + expressions `with` pour API fluente ; pas de mutation, pas de lifecycle sur les éléments
- `CompositeRenderElement.Add(Func<RenderElement>)` — les method groups fonctionnent dans les collection initializers
- `CompositeRenderElement.Add(UINode)` — sucre syntaxique vers `Add(new NodeElement(node))` ; `Add(IEnumerable<RenderElement>)` permet d'utiliser LINQ directement
- `Length.Auto = 0` en premier dans l'enum → `default(Length)` = Auto sans écriture explicite

### Composition et réconciliation
- `NodeElement(UINode Node) : RenderElement` — couture de composition : un UINode peut être enfant de layout d'un autre UINode avec état isolé. `LayoutEngine` résout `Node.GetElement()` à chaque frame via `while (el is NodeElement ne) el = ne.Node.GetElement()` ; `Effective(el)` consulte l'élément interne pour les décisions de layout (Fill, Anchor, marges) sans résolution complète.
- État hover/sélection stocké dans le `UINode` propriétaire (ex. `NavItemNode._hovered`) — un seul champ `Property<T>` suffit pour N items d'une liste, plus `When(bool, Func<T,T>)` pour le style conditionnel au moment du `Render()`
- `RenderElement` immuable = pas de `WhenHovered` draw-time (supprimé) — les propriétés qui affectent le layout ne peuvent pas être surchargées à l'étape de dessin sans recalcul ; le pattern correct est `UINode` + `MarkDirty()` + rebuild

### Events
- `InputPass` lit `UILayoutCache` (frame précédente) en update, `UIRenderPass` écrit en render — latence d'une frame imperceptible à 60+ fps
- Hit-testing : collecte récursive de tous les éléments sous le curseur (parents + enfants) ; pas encore de propagation/stopPropagation

### Animations (`Animations/`)
- **`IAnimation`** — interface de base : `IsComplete`, `Advance(delta)`
- **`IAnimation<T> : IAnimation`** — ajoute `T Value`
- **`Easing`** — static class : `Linear`, `EaseIn`, `EaseOut`, `EaseInOut` (`Func<float,float>`)
- **`Lerp`** — static class : `Float`, `Color` (RGBA byte-exact), `Vector2`
- **`Tween<T> : IAnimation<T>`** — interpolation explicite A→B : `To(target)` idempotent, interruption propre (`_from = Value` courant), duration/easing overridable par `To()`
- **`ImplicitAnimation<T> : IAnimation<T>`** — suit un `ReadOnlyProperty<T>` via `ValueChanged` ; démarre automatiquement à chaque changement, interruption propre

#### Intégration UINode / passes
- **`UINode.AddAnimation(IAnimation)`** — enregistre une animation (protected)
- **`UINode.AdvanceAnimations(double delta)`** — avance toutes les animations non-complètes et appelle `MarkDirty()` si au moins une est active (internal, appelé par `AnimationPass`)
- **`AnimationPass : IUpdatePass`** — passe `OnRender` qui traverse l'arbre et appelle `AdvanceAnimations` sur tous les UINodes
- Dans la démo : `AnimationPass` ajouté avant `UIRenderPass` dans la liste des passes

#### Pattern UINode avec Tween
- Déclarer `Tween<T>` en champ + `AddAnimation(tween)` dans le constructeur
- Dans `Render()` : `tween.To(targetValue)` — idempotent, donc sûr à appeler chaque frame
- `AnimationPass` avance le tween chaque frame ; quand non-complet → `MarkDirty()` → `UIRenderPass` re-rend
- **`NavItemNode`** : démontre `Tween<Color>` pour la transition hover bg+fg

### Layout caching
- `UINode.IsDirty` (`internal`) — lecture du flag `_dirty` sans effet de bord ; le flag passe à `false` lors du premier `GetElement()` dans `Layout()`
- `UIRenderPass._cache` — `Dictionary<UINode, (LayoutNode, vw, vh)>` ; skip layout si aucun UINode du sous-arbre de scène n'est dirty **et** viewport inchangé
- La propagation dirty est implicite : les UINodes embarqués (`NavItemNode`, etc.) sont des enfants dans l'arbre de scène → `GetSubtreeDepthFirst()` les atteint directement

---

## Prochaines étapes

1. **Propagation d'events** — `IAnimation<T>`, `Tween<T>`, `ImplicitAnimation<T>`, `AnimationPass`
3. **Propagation d'events** — bubble/capture, `StopPropagation`
4. **Injection de données** — équivalent `InheritedWidget` / Context pour theme, locale, sans prop drilling
5. **`TextBlock` min-content** — largeur minimale = mot le plus long, pour les parents fit-content
6. **`IRenderer` OpenGL** — implémentation directe sans Skia pour comparaison
7. **`UpdateFlag`** — contrôle update par sous-arbre
