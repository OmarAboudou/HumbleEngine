# Roadmap — Intégration SceneGraph ↔ Renderer

Objectif : relier l'arbre de scène à la boucle de rendu. Le `SceneTree` entre dans la
frame loop (`FlushDisposeQueue` au « safe point » promis par sa doc), et le dessin
devient *demandé par les nœuds* au lieu d'être câblé dans le backend — le triangle
quitte `VulkanRenderer` et devient un nœud du Sandbox. C'est le préalable de la
brique UI (quads, layout — hors périmètre ici, roadmap suivante).

Découpage en blocs — chaque bloc compile, **se voit** (Sandbox) et est validé avant le suivant.

## Décisions de conception

### L'architecture de dépendance (passe 1)

- **Le nœud dessine — modèle retenu** (validé 2026-06-12) : les nœuds visuels
  reçoivent `IRenderer` et émettent leurs commandes pendant le parcours de l'arbre.
  Nouvelle flèche `SceneGraph → HAL` — une flèche vers des *abstractions pures*
  (HAL ne référence aucun backend) : dépendre d'`IRenderer` n'est pas dépendre de
  Vulkan, et `Node` comme `IRenderer` vivent déjà dans l'API racine `HumbleEngine`.
- **Protocole interne + marcheur externe — rejeté** : un langage intermédiaire de
  commandes dupliquerait ce qu'`IRenderer` doit exprimer de toute façon — une
  abstraction sans second client (même raisonnement que le report de l'abstraction
  de shading en roadmap 06).
- **Serveur de rendu à la Godot — différé, noté comme évolution** : les nœuds ne
  dessinent jamais et *poussent* leurs mutations vers un monde retenu côté renderer
  (les canvas items du `RenderingServer`). Découplage total, rendu threadable, et
  affinité naturelle avec `Reactive<T>` (pousser au changement = un binding). Ses
  clients (render thread dédié, séparation éditeur/jeu) n'existent pas encore —
  on apprendra à faire vivre un monde avant d'en synchroniser deux.

### Le vocabulaire de soumission (passe 2)

- **Ressource + soumission** (validé 2026-06-12) — la paire fondatrice des API
  graphiques : créer les ressources d'avance (coûteux, rare), soumettre les draws
  (léger, chaque frame). `IRenderer.CreateMesh(vertices)` rend un handle opaque
  **`IMesh : IDisposable`**, possédé par l'appelant (le nœud le détruit dans
  `OnDispose`, avant le renderer — ordre inverse de création) ; **`Draw(mesh)`**
  n'est légal qu'entre `BeginFrame` et `EndFrame`. On expose la frontière qui
  existe déjà dans `VulkanRenderer` (`CreateVertexBuffer<T>` / bind + `vkCmdDraw`).
  Rejetés : le placebo `DrawTriangle()` (câblage déplacé d'un étage) et le langage
  complet matériaux/command lists (clients absents — übershader UI, vrais assets).
- **`Vertex` public dans HAL** — `Vector2 Position` + `Vector3 Color` (stride 20),
  l'actuel `TriangleVertex` promu au rang de contrat. **Première dépendance de
  HAL : HAL → Mathematics**, assumée — Mathematics est de la donnée pure sans
  dépendance, et un contrat graphique sans types mathématiques duplique des
  `float x, y`.
- **Sur `IRenderer` même, pas en capability** — le pattern `is` sert aux options
  matérielles ; dessiner est la raison d'être d'un renderer. `OpenGLRenderer`
  répondra par un `NotSupportedException` documenté : le backend OpenGL était le
  chantier d'apprentissage GLX, sa mise à niveau est un chantier séparé.
- **Le hook vit sur un sous-type `VisualNode`**, pas sur `Node` —
  `protected virtual OnDraw(IRenderer)` + point d'entrée interne pour le parcours
  (même mécanique qu'`EnterTree`). Notre propre règle l'impose : le `Node` de base
  ne porte pas de transform, dessiner est encore plus spécialisé. Choix de Godot
  également (`_draw()` sur `CanvasItem`). La brique UI y ancrera `UINode`.
- **Le renderer est injecté** — « dependencies are injected » : le Sandbox
  construira `TriangleNode(renderer)`, qui crée son mesh au constructeur, dessine
  dans `OnDraw`, détruit dans `OnDispose`. Aucune magie d'ambiance.

### L'anatomie d'une frame (passe 3)

- **L'ordre canonique** (validé 2026-06-12) : `PollEvents` (caché dans `Window.Run`)
  → `BeginFrame` → `tree.Render(renderer)` → `EndFrame` → `Present` →
  `tree.FlushDisposeQueue()`. **Le flush va en fin de frame** : le moment sûr est
  celui où plus aucun code utilisateur ne tourne. Sémantique résultante : un nœud
  qui fait `QueueDispose` en frame N se dessine encore en N et meurt avant la N+1 —
  le `queue_free()` de Godot.
- **Pas de hook d'update** dans cette roadmap — le triangle n'a rien à mettre à
  jour, `OnFrame(delta)` serait une abstraction sans client. Ses clients
  (animations, layout UI, routage d'input) arrivent avec la brique UI ; sa place
  est réservée entre `PollEvents` et `BeginFrame`. **Extension différée.**
- **Le parcours appartient à `SceneTree`** (`tree.Render(renderer)`, mécanique
  interne à l'assembly comme `EnterTree`), **la composition appartient à
  l'application** — pas d'`EngineContext`, le Sandbox écrit la partition lui-même,
  cinq lignes visibles. Le renderer est passé en argument à chaque appel, jamais
  retenu : le tree reste sans état de rendu. Sa doc passe de « knows nothing about
  windows or rendering » à « knows nothing about windows ».
- **Destruction** : l'arbre meurt avant le renderer (les `OnDispose` détruisent
  leurs `IMesh`) — l'ordre inverse de création donne `tree → renderer →
  graphicsBackend → window → windowBackend`, sans amendement de la règle.

## Blocs

- [x] **Bloc 1 — Conception** ✅ (passes progressives)
  - [x] Passe 1 : l'architecture de dépendance ✅ → section « L'architecture de
    dépendance » ci-dessus
  - [x] Passe 2 : le vocabulaire de soumission ✅ → section « Le vocabulaire de
    soumission » ci-dessus
  - [x] Passe 3 : l'anatomie d'une frame ✅ → section « L'anatomie d'une frame »
    ci-dessus

- [x] **Bloc 2 — La boucle** ✅ — `SandboxScene` (vide) narrant son cycle de vie sur
  la console, `FlushDisposeQueue` après `Present`, destruction `tree → renderer →
  backend → window → windowBackend` ; doc de `FlushDisposeQueue` mise à jour
  (promesse « later wired » tenue). Cycle complet attach/detach/dispose confirmé
  au Sandbox, 186 tests verts

- [x] **Bloc 3 — Le dessin piloté par l'arbre** ✅ — `Vertex` + `IMesh` +
  `CreateMesh`/`Draw` dans HAL (flèche HAL → Mathematics assumée),
  `VulkanMesh` (dispose = device idle puis destroy — le stall est la réponse
  pédagogique, la destruction différée viendra avec les frames in flight),
  `VisualNode`/`Node.RenderSubtree`/`SceneTree.Render` dans SceneGraph
  (flèche SceneGraph → HAL), `NotSupportedException` documentée côté OpenGL,
  `TriangleNode(renderer)` au Sandbox. Triangle identique, mort à 5 s via
  `QueueDispose` confirmée au Sandbox (validation muette), compteur fps ajouté
  (144 fps constants = vsync FIFO du moniteur)

- [x] **Bloc 4 — Tests** ✅ — unitaires (FakeRenderer/FakeMesh/TestVisualNode :
  ordre du peintre, nœuds logiques muets, renderer jamais retenu, sémantique
  frame N/N+1 de QueueDispose avec mort du mesh) + intégration Vulkan
  (CreateMesh, Draw multi-frames, Draw hors frame, mesh étranger, dispose en
  pleine boucle, arbre → renderer de bout en bout). 194 + 47 tests verts

---

## Résultat

L'arbre pilote le dessin : `SceneTree` vit dans la boucle de frame (flush des
disposals en fin d'itération — un nœud queued en frame N se dessine en N et meurt
avant la N+1), les `VisualNode` soumettent leurs draws pendant `tree.Render(renderer)`
(ordre du peintre, renderer injecté jamais retenu), et le contrat ressource +
soumission (`Vertex`, `IMesh`, `CreateMesh`/`Draw`) relie SceneGraph au backend
Vulkan à travers les abstractions de HAL. Le triangle est devenu un citoyen de
l'arbre : il naît avec son nœud, se dessine par la traversée, meurt avec lui.

**Extensions différées** (chacune attendra son client) : hook d'update
`OnFrame(delta)` (clients : animations, layout UI, input) ; serveur de rendu à la
Godot (clients : render thread, éditeur — affinité Reactive notée) ; destruction
différée des meshes (client : frames in flight) ; mise à niveau du backend OpenGL
au contrat de dessin (si jamais).

*Tâche terminée*
