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
- **Différés** : IME/composition (client : l'international), scale factor
  HiDPI Wayland, hooks par type sur la fenêtre.

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

- [ ] **Bloc 3 — Les événements naissent** — `InputEvent` (hiérarchie de
  records) dans HAL, canal `OnInput`, traduction pointeur X11 (masque +
  cases) et Wayland (`wl_seat` + listeners pointer) ; démo : les événements
  défilent en console depuis les deux fenêtres

- [ ] **Bloc 4 — Le routage** — `tree.RouteInput`, hit-test inverse du
  peintre, bubbling, capture implicite, hover synthétisé ; démo : les tuiles
  de la colonne réagissent au survol, un clic en tue une (`QueueDispose`) et
  la `Column` se resserre toute seule

- [ ] **Bloc 5 — Le clavier** — enum `Key`/`KeyModifiers`, X11
  (`XLookupString`), Wayland (xkbcommon), focus, `TextInput`, répétition
  client-side ; démo : déplacer le panneau aux flèches

- [ ] **Bloc 6 — Tests** — unitaires (routage sur arbre + FakeRenderer, sans
  display) + intégration Linux, fin de roadmap

*Tâche en cours*
