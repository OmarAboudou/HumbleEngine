# Roadmap — La brique UI, premier étage (quads, pixels, layout)

Objectif : le premier étage de la brique UI — **un panneau coloré, en pixels,
piloté par le layout de l'arbre**. Le renderer apprend le quad et l'espace pixel
(l'übershader UI naît ici, en mode « couleur pleine »), le SceneGraph gagne
`UINode` (hérite de `VisualNode`) avec un `Rect` réactif, et un premier conteneur
place ses enfants.

Hors périmètre assumé (chacun viendra avec son étage) : input/hit-testing et le
routage des événements fenêtre → arbre (client du two-way binding : champ texte),
texte, textures, `Switcher`/`ListPanel` complets.

Découpage en blocs — chaque bloc compile, **se voit** (Sandbox) et est validé avant le suivant.

## Décisions de conception

### L'anatomie d'un renderer UI (passe 1)

- **Conversion pixel → clip côté GPU, matrice orthographique** (validé 2026-06-12) :
  les sommets restent en pixels, le vertex shader multiplie par
  `Matrix4x4.CreateOrthographic` (le rôle que la roadmap 06 lui prédisait).
  Un resize = 16 floats changent. Côté CPU rejeté : le viewport s'invite dans
  chaque écriture de sommet, et un resize invaliderait toute la géométrie.
  Le choix de Dear ImGui et du canvas Godot.
- **Géométrie dynamique : quad unitaire immortel + paramètres par draw** —
  le renversement : la géométrie ne change jamais (un quad 0..1 partagé, voire
  généré dans le vertex shader sans buffer — le « triangle sans données » de la
  roadmap 06), seuls les *paramètres* du draw changent (« ce rect, cette couleur,
  ce mode »). Un draw par panneau, zéro réécriture de buffer, le modèle naturel
  du mini-übershader. Rejetés : recréer le mesh à chaque changement (un
  `vkDeviceWaitIdle` par resize de panneau) ; le batching à la Dear ImGui (un
  gros buffer réécrit par frame, un seul draw) — la cible performance des UI
  réelles, **différé avec son client** (des centaines de quads). Godot fait les
  deux. Le chemin mesh de la roadmap 07 reste celui des géométries immobiles.

### L'übershader UI et le chemin des données (passe 2)

- **Tout en push constants, zéro descriptor** (validé 2026-06-12) — les trois
  routes vers un shader : attributs de vertex (roadmap 07, hors sujet : le quad
  n'a plus de données par sommet), **push constants** (128 o garantis, écrits
  dans le command buffer par `vkCmdPushConstants`, granularité par draw, zéro
  allocation/synchro), uniform buffers + descriptors (la grosse cérémonie —
  **différée avec son client incontournable : les textures**, prochain étage).
  Budget : matrice 64 + rect 16 + couleur 16 + mode 16 (aligné) = 112 ≤ 128.
- **La matrice ortho poussée une fois par frame** (offset 0, stage vertex) —
  les push constants persistent entre les draws d'un command buffer ; chaque
  `DrawQuad` ne pousse que ses 48 octets (offset 64). Deux plages au pipeline
  layout, stage flags distincts. `Matrix4x4.CreateOrthographic(0, w, h, 0, 0, 1)`
  — origine en haut à gauche, Y vers le bas, le rôle prédit en roadmap 06.
- **L'übershader à ensemble fermé** — `ui.vert` : aucun `layout(in)`, quad
  unitaire né de `gl_VertexIndex` (6 sommets), étiré sur le rect puis multiplié
  par l'ortho ; UV 0..1 en sortie (gratuites, pour les modes futurs).
  `ui.frag` : `switch` sur le mode poussé — **mode 0 : couleur pleine**, seul
  mode de cette roadmap ; texture/texte/coins arrondis s'ajouteront dans le
  même shader. Un seul PSO pour toute l'UI, pas d'explosion combinatoire.
- **Deuxième PSO, alpha blending allumé** — la promesse de la roadmap 06
  (« le blending se réveillera pour l'UI ») : panneaux translucides, puis
  antialiasing du texte. Couleur en **`Vector4` RGBA**. Aucun vertex input,
  viewport/scissor dynamiques comme le pipeline mesh.
- **API HAL : `DrawQuad(Rect rect, Vector4 color)`** sur `IRenderer`, légal
  entre `BeginFrame` et `EndFrame`. Le « mode » reste interne au renderer tant
  qu'un seul existe. Un `DrawQuad` = bind pipeline quad + push 48 o +
  `vkCmdDraw(6)`.

### `UINode` et le modèle de layout (passe 3)

- **Coordonnées relatives au parent** (validé 2026-06-12) — `UINode` porte
  `Position` (relative au parent UI) et `Size`, en pixels ; le rect absolu est
  résolu en remontant les ancêtres au moment du dessin (O(profondeur), pas de
  cache — le dirty flag de Godot attendra ses arbres profonds). Stocker l'absolu
  ferait réécrire toute la descendance quand un conteneur bouge.
- **`Position` et `Size` en `Reactive<Vector2>`** (via `CreateReactive`) — deux
  cellules séparées, pas un `Reactive<Rect>` atomique : le layout écrit
  `Position` sans toucher `Size`, l'application binde l'un sans l'autre. La
  réactivité n'est **pas nécessaire au rendu** (on redessine tout chaque frame,
  `OnDraw` lit `.Value`) : les cellules sont la surface de **binding** (cap
  `ObservableProperty`) et le moteur du **layout**. Damage tracking différé.
- **Taxonomie** : `UINode : VisualNode` (ne dessine rien, comme le `Control`
  Godot) → `Panel : UINode` (`Color` réactif `Vector4`, `OnDraw` →
  `DrawQuad`) → conteneurs **`Column`/`Row`** (vocabulaire Flutter — `StackPanel`
  rejeté : « stack » évoque l'empilement en Z) : empilent leurs enfants sur un
  axe avec espacement, composition publique via `NodeList<UINode>` (les
  conteneurs ouvrent, les scènes ferment).
- **Layout réactif, pas par frame** — le conteneur recalcule les `Position` de
  ses enfants quand l'observable change : sa liste d'enfants (`NodeList`
  observable, machinerie payée) ou la `Size` d'un enfant. Pas de boucle
  possible : le layout écrit `Position`, ne dépend que de `Size` et de l'ordre.
  Le layout différé batché en fin de frame (à la Godot) attendra les cascades.
- **Hors étage** : sizing par contenu (client : le texte), anchors/stretch, input.

## Blocs

- [x] **Bloc 1 — Conception** ✅ (passes progressives)
  - [x] Passe 1 : l'anatomie d'un renderer UI ✅ → section « L'anatomie d'un
    renderer UI » ci-dessus
  - [x] Passe 2 : l'übershader UI ✅ → section « L'übershader UI et le chemin
    des données » ci-dessus
  - [x] Passe 3 : `UINode` et le modèle de layout ✅ → section « UINode et le
    modèle de layout » ci-dessus

- [x] **Bloc 2 — Le quad** ✅ — `vkCmdPushConstants`/`VkPushConstantRange`/blend
  enums au P/Invoke, shaders `ui.vert`/`ui.frag`, `VulkanPipeline` refactoré en
  cœur paramétré + deux entrées (mesh, quad), `QuadPush`/`QuadParams` (géographie
  du bloc 112 o), bind paresseux des pipelines, matrice ortho poussée par frame
  (copie brute — Mathematics est column-major exprès), `DrawQuad(Rect, Vector4)`
  sur `IRenderer`. Simplification vs passe 2 : **une seule plage push V+F 0..112**
  (deux plages à stages distincts = subtilités de compatibilité entre blocs par
  étage, sans bénéfice à notre échelle) ; les deux cadences d'écriture demeurent.
  Panneau translucide au-dessus du triangle confirmé au Sandbox (blending
  « over » visible), 241 tests verts, validation muette

- [x] **Bloc 3 — `UINode`** ✅ — `UINode` (Position/Size en `Reactive<Vector2>`
  via `CreateReactive`, `GlobalRect` résolu en remontant les ancêtres UI, les
  nœuds non-UI transparents) et `Panel` (premier nœud UI concret du moteur,
  `Color` réactif RGBA, scellé tant que rien n'en hérite). Au Sandbox : le
  `DrawQuad` direct remplacé par un `Panel` dans l'arbre (attaché après le
  triangle — ordre du peintre), `PanelPosition` exposé en contrat typé de la
  scène, animé au sinus : le panneau glisse parce qu'une cellule change.
  Confirmé au Sandbox, validation muette

- [x] **Bloc 4 — Premiers conteneurs** ✅ — `LinearContainer` (mécanique
  partagée : `Children` public en `NodeList<UINode>` — les initialiseurs de
  collection marchent —, `Spacing` réactif, relayout branché sur trois
  observables : la liste y compris départs dans son dos, `Size` de chaque
  enfant abonnée à l'entrée/désabonnée à la sortie, `Spacing` ; aucune passe
  par frame) ; `Column`/`Row` scellés (`Place` + `MainExtent`, quatre lignes
  chacun, pas de stretch transversal). Au Sandbox : colonne de trois tuiles,
  celle du milieu respire, celle du dessous suit toute seule. Confirmé,
  validation muette

- [x] **Bloc 5 — Tests** ✅ — unitaires (`UINodeTests` : GlobalRect sans/avec
  ancêtres UI, ancêtres non-UI transparents, dessin du `Panel` vers
  `FakeRenderer.Quads`, cellules comme surface de binding, libération des
  bindings à la mort ; `LinearContainerTests` : empilement Column/Row avec
  spacing, relayout sur les trois observables — taille d'enfant, spacing,
  retrait avec désabonnement, départ dans le dos de la liste —, conteneurs
  imbriqués sans taille propre) + intégration Vulkan (quads + mesh dans la même
  frame — le bind paresseux bascule deux fois —, DrawQuad hors frame).
  207 + 49 tests verts

---

## Résultat

Le moteur a son premier étage d'UI : un chemin quad pixel-space dans le renderer
(übershader à ensemble fermé — mode 0 couleur pleine —, tout en push constants,
alpha blending « over », zéro buffer, zéro descriptor), `UINode` (Position/Size
réactifs, GlobalRect par remontée d'ancêtres), `Panel` (premier nœud concret du
moteur), et `Column`/`Row` au layout réactif branché sur la narration des
observables — aucune passe de layout par frame. Un panneau bouge parce qu'une
cellule change : la promesse des bindings tenue à l'écran.

**Extensions différées** (chacune avec son client) : batching des quads
(client : des centaines de panneaux) ; descriptors/uniform buffers (client :
les textures — texte, images) ; modes 1+ de l'übershader (texture, glyphes,
coins arrondis) ; sizing par contenu et stretch/anchors (client : le texte,
les vraies mises en page) ; cache du rect global à dirty flag (client : arbres
profonds) ; layout différé batché en fin de frame (client : cascades).
**Prochains étages de la brique UI** : le routage de l'input fenêtre → arbre
(hit-testing — le client du two-way binding : champ texte), puis le texte.

*Tâche terminée*
