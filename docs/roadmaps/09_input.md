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
  210 + 50 tests verts, validation muette

- [ ] **Bloc 2 — Conception input** (passes progressives, à mener après le
  branchement)
  - [ ] Passe 2 : les événements fenêtre — ce que les backends X11/Wayland
    publient (souris, clavier), la forme HAL des événements
  - [ ] Passe 3 : hit-testing et routage — qui traverse, dans quel ordre
    (z inverse du peintre), capture/bubbling, focus

- [ ] **Blocs suivants** — découpés à l'issue des passes 2-3

*Tâche en cours*
