# Roadmap — L'input (fenêtre → arbre)

Objectif : les événements naissent dans la fenêtre et atteignent les nœuds —
publication des événements clavier/souris par les backends fenêtrage,
hit-testing, routage dans l'arbre. C'est le client du two-way binding réel
(champ texte). Préalable structurel traité en premier : **le trio
fenêtre–renderer–arbre**, que l'input et le rendu interrogent chacun de leur
côté.

Découpage en blocs — chaque bloc compile, **se voit** (Sandbox) et est validé avant le suivant.

## Décisions de conception

### Le trio fenêtre–renderer–arbre (passe 1)

- **Une fenêtre ↔ un renderer ↔ un arbre, et le trio devient typé** (validé
  2026-06-12) : `SceneTree(renderer)` — l'application injecte, l'arbre retient.
  `tree.Render()` perd son paramètre (une seule source de vérité). Le
  multi-fenêtre d'aujourd'hui = N trios côte à côte, câblés par l'application.
- **Les ressources GPU des nœuds naissent à l'attach, meurent au detach** —
  l'argument décisif est le cap produit : **l'éditeur doit instancier tout nœud
  sans paramètre** (constructeur par défaut, propriétés sérialisées — le contrat
  Godot). Donc plus d'injection de renderer au constructeur (`TriangleNode()`
  redevient pur) : `OnAttached` crée, `OnDetached` libère — pile dans la
  sémantique existante (« hooks fire on every entry/exit »), et `Dispose`
  détache avant de disposer, donc un seul chemin de libération. Bonus éditeur :
  la scène éditée vivra dans l'arbre de l'éditeur et acquerra ses ressources
  du contexte de l'éditeur — ce qui rend la preview possible.
- **Un seul point d'accès au contexte de rendu** : `VisualNode.Renderer`
  (protégé) — aujourd'hui résolu en `Tree?.Renderer`, demain en « plus proche
  ancêtre viewport ». Les nœuds ne codent jamais la résolution en dur : la
  migration future est un changement de plomberie, pas une réécriture.
- **La garde d'origine devient réelle** : `VulkanMesh` retient son renderer
  créateur, `Draw` rejette un mesh étranger par `ArgumentException` — le
  mélange inter-devices était un crash Vulkan latent.
- **Destination gravée : le modèle `WindowNode`/viewport à la Godot**
  (proposé par Omar, validé comme cible) — un arbre unique, les fenêtres comme
  nœuds (`Window` hérite de `Viewport` chez Godot), chaque viewport rend son
  sous-arbre sans traverser les viewports imbriqués, le contexte de rendu d'un
  nœud = son plus proche ancêtre viewport, propriété renderer/backend sur le
  nœud avec défaut hérité. Achète : les scènes déclarent des fenêtres
  (dialogues, docks — sérialisables par l'éditeur), un seul monde (une passe
  d'update, un flush, un pipeline d'input). Coûte : double nature OS/nœud des
  fenêtres, pompe d'événements multi-fenêtres (chantier HAL), et une
  notification « viewport changé » (« moving is not leaving » ne rejoue pas
  attach/detach lors d'un reparentage intra-arbre — il en faudra une pour
  réacquérir les ressources). **Différé avec son client : l'éditeur / le vrai
  multi-fenêtre.** Le point d'accès unique ci-dessus est ce qui rend la
  bascule indolore.
- Note connexe différée : les popups/menus de l'UI pourront d'abord être des
  overlays dans la même fenêtre (fin d'ordre du peintre) avant d'exiger de
  vraies fenêtres.

### Les événements fenêtre (passe 2)

- **Une couche d'abstraction, un canal unique** (validé 2026-06-13 — proposé
  par Omar) : `IWindow.OnInput` (`event Action<InputEvent>`). Les backends *sont* la couche
  de traduction : dialecte natif (`XEvent`, `wl_pointer`/`wl_keyboard`) →
  vocabulaire HAL unique. Justification : **le routage est le client de
  l'abstraction** — N événements typés sur la fenêtre imposeraient N chemins
  de routage dans l'arbre (hit-test, capture, bubbling dupliqués) ; c'est la
  vraie raison d'être de l'`InputEvent` de Godot (`_input(event)`, un tuyau).
- **`InputEvent` = hiérarchie de records** (validé 2026-06-13 — proposé par
  Omar, contre la struct union d'abord envisagée) : `InputEvent` abstrait →
  `PointerEvent(Vector2 Position)` abstrait → records scellés précis
  (`PointerMoved`, `PointerPressed(Button, Position)`, …). Achète : la
  précision par type (un Moved n'a pas de champ Button), le pattern matching
  (`e is PointerPressed { Button: Left }`), **l'étage intermédiaire dont le
  routeur a besoin** (`e is PointerEvent p` → hit-test sur `p.Position`,
  sans connaître le genre), l'égalité structurelle pour les tests, un record
  de plus par genre futur. Coût re-mesuré et accepté : ~1000 evts/s × ~50 o
  = bruit en Gen0 (WPF, Avalonia, Godot allouent pareil) ; pooling localisé
  (backends + routeur) si un profil proteste un jour.
- **Kinds** : pointeur complet d'abord — `PointerMoved/Pressed/Released/
  Scrolled/Entered/Exited` (coordonnées pixel surface-local, origine
  haut-gauche : déjà l'espace UI sur les deux backends) ; clavier ensuite en
  **deux canaux conceptuels** — `KeyPressed/Released` (touche logique : enum
  `Key` + `KeyModifiers`, pour flèches/raccourcis) et `TextInput` (caractère
  composé : layout, touches mortes — ce que consommera le champ texte, jamais
  les touches brutes).
- **Réalités backend** : X11 facile (étendre le masque — `PointerMotionMask`
  absent aujourd'hui —, des cases dans `HandleEvent`, `XLookupString`) ;
  Wayland est le morceau — binder `wl_seat`, listeners pointer/keyboard,
  **xkbcommon** à P/Invoke (keymap fd → état → keysym + UTF-8), et la
  **répétition de touches à la charge du client** (le compositeur n'envoie
  que `repeat_info`) — notée pour le bloc clavier (maintenir Backspace).
- **Le confort typé au bon étage** : `UINode` dispatche depuis son entrée
  unique vers des hooks de convenance typés (passe 3).
- **Le canal vit sur `IGraphicsSurface`, pas `IWindow`** (amendé 2026-06-13,
  question d'Omar sur l'unification desktop/mobile) : toute surface publie le
  même vocabulaire — le modèle **W3C Pointer Events** : un « pointeur » est
  tout ce qui pointe (souris, doigt, stylet) ; contre-exemple Godot, événements
  séparés rafistolés par deux réglages d'émulation croisée. **Mêmes mots,
  grammaires différentes** : souris = pointeur persistant (Moved sans contact,
  hover) ; toucher = pointeur transitoire (né au Pressed, Moved = drag, mort
  au Released — entre deux contacts, pas de position). Discipline gravée :
  ne jamais supposer qu'un Moved/Entered précède un Pressed.
- **Pas de second bouton au doigt** : les alternatives tactiles (appui long =
  le clic droit du tactile, double-tap, pinch) sont des **gestes** — motifs
  reconnus au-dessus du flux brut, couche universelle (le double-clic desktop
  aussi) — différés avec leur client (modèle Flutter : recognizers + arène).
- **Différés (ère mobile)** : `PointerId` (multi-touch — capture par
  pointeur dans le routeur), `PointerDeviceKind` (mouse/touch/pen), pression
  stylet, scroll cinétique synthétisé. Et toujours : IME/composition, scale
  factor HiDPI Wayland.
- **Contrat de grammaire tactile (question d'Omar, 2026-06-13)** : un doigt qui
  se lève = `Released` **puis `Exited` synthétisé par le backend** (le W3C
  émet `pointerleave` après `pointerup` pour le tactile, jamais pour la
  souris). Sans ça, `LastPosition` survivrait au doigt dans le routeur →
  hover fantôme au `RefreshHover`. Le routeur reste aveugle aux périphériques.
  Note voisine : à l'ère des doigts-sources, retirer l'état d'un pointeur
  transitoire à son `Exited` (sinon le dictionnaire par source fuit lentement).
- **Input direct à l'OS — complément, jamais remplacement (question d'Omar,
  2026-06-13)** : lire `/dev/input` exige root/groupe `input` (un lecteur de
  tous les claviers = un keylogger — le modèle de sécurité Wayland existe
  contre ça), et le routage refait à l'aveugle ce que seul le compositeur
  sait (focus, stacking, position du curseur accéléré — evdev n'a que du
  relatif). L'input fenêtré vient du fenêtrage ; le canal OS direct est
  l'opt-in du non-fenêtré : manettes, raw input des jeux (ère 2D).
- **Bug connu, différé avec diagnostic (vu par Omar)** : fenêtre Wayland
  réduite = compositeur cesse de composer = la WSI Vulkan bloque dans
  acquire/present = la boucle séquentielle gèle l'autre fenêtre. Correction
  nommée : remonter l'état `suspended` (configure xdg / libdecor) en
  `IWindow.IsSuspended`, et la boucle saute le *rendu* du trio suspendu en
  continuant de *pomper* ses événements. Croisera la refonte de pompe de
  l'ère WindowNode.
- **Multi-périphérique (discussion 2026-06-13)** : pas de dialectes parallèles
  ni de double émission — le spécifique est un champ, un record de plus, ou un
  événement *sémantique* d'étage supérieur (le « click » = press+release
  interprété, vivra sur `Button`, famille des gestes). Si une identité de
  périphérique devient nécessaire, **le pattern gravé est l'héritage d'Omar** :
  `MouseMoved(Mouse, …) : PointerMoved(…)` — un seul événement instancié,
  visible à deux altitudes, le routeur matche la base. Clients réels : souris/
  claviers fusionnent au seat (jamais d'identité probable) ; doigts =
  `PointerId` ; **manettes = la vraie branche multi** (ère 2D) — nouvelle
  famille `Gamepad*` portant son périphérique nativement, et venant d'une
  source OS, pas d'une surface (modèle SDL).

### Hit-testing et routage (passe 3)

- **L'application branche, symétrie avec le rendu** (validé 2026-06-13) :
  `window.OnInput += e => tree.RouteInput(e)` — l'arbre ne connaît pas la
  fenêtre, chaque fenêtre route vers son arbre (le bi-fenêtre du Sandbox
  marche sans une ligne de plus).
- **Hit-test = la traversée de `Render`, inversée** : derniers enfants
  d'abord, profondeur d'abord, premier `GlobalRect.Contains` gagnant — la
  convention demi-ouverte de `Rect.Contains` avait été écrite pour ce moment
  (« no double hit »). Nœuds non-UI transparents, comme au rendu.
- **Bubbling à retour booléen** : l'événement est offert à la cible puis
  remonte les ancêtres `UINode` jusqu'à consommation —
  `protected virtual bool OnInput(InputEvent e)`, `true` = consommé (les
  records sont immuables : le retour est le signal, pas de `Handled`
  mutable). Différés : la phase de capture à la WPF (client : drag-scroll
  parental), les hooks de convenance typés (client : `Button`).
- **Le routeur a trois états**, internes au `SceneTree` (même altitude que
  la dispose queue ; API publique : `tree.RouteInput`) : la **capture
  implicite du pointeur** (au `Pressed`, les `Moved`/`Released` vont au même
  nœud jusqu'au `Released`, même hors de son rect — sans ça ni bouton correct
  ni drag) ; le **hover synthétisé** (`Entered`/`Exited` *par nœud* dérivés
  des `Moved` ; ceux de la fenêtre ne font que réinitialiser) ; le **focus
  clavier** (principe : pas de hit-test, le focusé puis bubble — pris au
  clic, donné par code ; détails au bloc clavier).

## Blocs

- [x] **Bloc 1 — Le branchement** ✅ — `SceneTree(renderer)` (le trio typé) +
  `Render()` sans paramètre, protocole `VisualNode.Renderer` (résolution
  `Tree?.Renderer`, point unique pour la bascule viewport future),
  `TriangleNode`/`SandboxScene` défaut-constructibles (mesh acquis dans
  `OnAttached`, libéré dans `OnDetached` — chemin de libération unique, `Dispose`
  détachant d'abord), garde d'origine dans `Draw` (`VulkanMesh.Owner`, mesh
  étranger → `ArgumentException` au lieu d'un crash Vulkan latent). Tests :
  cycle acquisition/libération/réacquisition, protocole nul hors arbre, garde
  inter-renderers (deux fenêtres réelles). Écran strictement identique,
  210 + 50 tests verts, validation muette.
  **Démo bi-trio au Sandbox** (demande d'Omar) : deux fenêtres Wayland + X11,
  deux `SceneTree`, un seul backend Vulkan (l'instance activait déjà les deux
  extensions de surface) — même template `SandboxScene` instancié deux fois,
  animations en opposition de phase. Pièce moteur, en deux refactorings
  proposés par Omar : **la boucle en trois étages composables** —
  `IWindow.PollEvents` public (la primitive de pompe, desktop ; contrat
  main-thread documenté, plus caché), **`IGraphicsSurface.Step(onFrame)`**
  (une itération : pomper puis frame ; retourne true tant que la surface
  continue — idiome `MoveNext` ; chaque famille de surface implémente la
  sienne, une plateforme à boucle imposée par l'OS overridera `Run`), et
  `Run` réduit à une **default interface method** : `while (Step(onFrame))`.
  Multi-fenêtre = un `Step` par fenêtre dans la condition du `while` de
  l'application. Un éphémère `Window.RunAll` essayé puis supprimé : sa
  politique rigide et son cast vers `Window` signalaient la mauvaise altitude

- [x] **Bloc 2 — Conception input** ✅ (passes progressives)
  - [x] Passe 2 : les événements fenêtre ✅ → section « Les événements
    fenêtre » ci-dessus
  - [x] Passe 3 : hit-testing et routage ✅ → section « Hit-testing et
    routage » ci-dessus

- [x] **Bloc 3 — Les événements naissent** ✅ — hiérarchie `InputEvent` dans
  `HAL/Input/` (base → `PointerEvent(Position)` → records scellés ; conventions
  documentées : pixels surface-local, scroll +Y haut normalisé en crans,
  grammaires souris/toucher), canal `OnInput` sur `IGraphicsSurface` +
  `RaiseInput` sur `Window`. X11 : masque étendu (motion, enter/leave),
  boutons 1-3, molette 4-7 (press seul). Wayland : `wl_seat` **bindé en v1
  délibérée** (exactement les cinq événements pointer v1, pas de frame
  batching), capability → `wl_pointer`, conversion `wl_fixed` 24.8,
  **filtrage par surface** (libdecor partage le seat), proxys v1 détruits par
  `wl_proxy_destroy` (pas de destructeur en v1). Démo : log des records des
  deux fenêtres (Moved throttlés). Validé interactivement par Omar sur les
  deux backends, 260 tests verts

- [x] **Bloc 4 — Le routage** ✅ — `IDeviceEvent<out T>` (covariance →
  `IDeviceEvent<object>`, la clé du routeur), jeton `Mouse` (granularité
  « best effort » documentée), sous-records `Mouse*` héritant des neutres
  déscellés (le pattern d'Omar : un événement, deux altitudes), backends
  émettant avec leur jeton. `Node.HitTest` (rendu inversé, sans clipping),
  `UINode.OnInput → bool`, `InputRouter` interne (état **par source** :
  hover + capture implicite par pointeur, garde `Alive` contre les nœuds
  morts en plein drag, hover node-scoped synthétisé sans bubbling),
  `SceneTree.RouteInput`. **Correctif post-validation (bug vu par Omar)** :
  le hover est un état dérivé (pointeur × géométrie) — `Render()` termine
  par `RefreshHover` sur la dernière position connue de chaque pointeur
  (gelé sous capture) : une tuile qui glisse sous un curseur immobile
  gagne/perd le hover. Démo : tuiles hover + clic = `QueueDispose`, la
  colonne se resserre. Validé interactivement, 260 tests verts

- [x] **Bloc 5 — Le clavier** ✅ — `Key`/`KeyModifiers` + records en deux
  canaux (`KeyEvent` → `KeyPressed`/`KeyReleased` ; `TextInput` jamais dérivé
  des touches), jeton `Keyboard` + sous-records, **`KeysymTranslation`
  partagée** (X11 et xkbcommon = même dialecte keysym, héritage XKB). X11 :
  `XLookupString` (texte Latin-1 — l'UTF-8 complet exige XIM, différé avec
  l'IME), auto-repeat accepté. Wayland : **`XkbNative`** (P/Invoke
  libxkbcommon — le compositeur n'envoie que scancodes + fd de keymap,
  l'interprétation est au client), `wl_keyboard` v1 (keymap mmap→compile→état,
  enter/leave filtrés par surface, scancode+8, modificateurs par
  `update_mask`/noms XKB), pas de répétition (différée : champ texte).
  SceneGraph : **focus** (`GrabFocus`, `tree.FocusedNode`, famille clavier →
  focusé puis bubble, jamais de hit-test, un focus actif). Démo :
  `MovablePanel` aux flèches (Shift = ×4), accents composés vérifiés.
  Validé interactivement sur les deux backends

- [x] **Bloc 6 — Tests** ✅ — `InputRouterTests` (sur arbre + `FakeRenderer`,
  sans display) : hit-test reverse-painter (le plus haut gagne, bubbling aux
  seuls ancêtres `UINode`, nœuds logiques transparents), bubbling booléen
  s'arrêtant à la consommation, capture implicite (Moved/Released au nœud
  pressé hors de son rect), hover synthétisé node-scoped sans bubbling,
  `RefreshHover` suivant le monde sous un pointeur immobile, focus clavier
  (famille clavier jamais hit-testée, focusé puis bubble), deux souris
  fantômes à hover/capture indépendants (état par source via le jeton
  covariant), nœuds morts cessant de recevoir focus et capture. **219 tests
  unitaires verts.** Un correctif d'attente au passage (`TwoPhantomMice`) :
  le log enregistre le type runtime, donc un `MouseMoved` bubblé se loggue
  `MouseMoved` et non `PointerMoved` — c'est le pattern bloc 4 « un événement,
  deux altitudes » ; seul le hover *synthétisé* par le routeur reste le neutre
  `PointerEntered`/`Exited`.
  - **Pas d'intégration input, décision assumée (validé 2026-06-13)** :
    l'asymétrie est dure. **Wayland est non-instrumentable côté client** — le
    compositeur a le monopole de l'input (le modèle de sécurité même cité plus
    haut : injecter = keylogger), pas de synthèse sans compositeur headless
    dédié. **X11 serait pilotable** (`XSendEvent` dans notre propre queue,
    non-intrusif — `HandleEvent` ne filtre pas `send_event` ; ou `XTEST`,
    intrusif car bouge le vrai curseur, fragile sous Xwayland) mais ne couvrirait
    qu'un seul des deux backends. La traduction dialecte natif → vocabulaire HAL
    reste donc **couverte par la validation interactive des blocs 3-5** (les deux
    backends, sur la vraie machine), comme déjà gravé. Voie d'instrumentation X11
    notée si le besoin se représente.

*Roadmap terminée.*
