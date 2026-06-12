# Roadmap — Le triangle Vulkan (pipeline graphique)

Objectif : faire passer le backend Vulkan du *ménage* (`vkCmdClearColorImage`, chemin
transfer) au *dessin* — shaders, pipeline, `vkCmdDraw` — jusqu'à un triangle coloré à
l'écran, puis le même triangle alimenté par un vertex buffer (première mémoire GPU).
C'est le chantier qui débloquera l'intégration SceneGraph↔renderer et le rendu des
quads UI (hors périmètre ici, roadmaps suivantes).

Découpage en blocs — chaque bloc compile, **se voit** (Sandbox) et est validé avant le suivant.

## Décisions de conception

- **Dynamic rendering, pas de render pass** (validé après discussion, 2026-06-12) —
  le render pass est le « plan de vol » déclaré d'avance des GPU à tuiles (mobiles) :
  `loadOp`/`storeOp` pilotent le préchargement/réécriture des tuiles, les subpasses
  chaînent des étapes sans quitter la puce. Sur nos GPU de bureau (pas de tuiles), le
  driver le traduit… en barriers — que notre code écrit déjà à la main. Vulkan 1.3
  (`vkCmdBeginRendering`) supprime la cérémonie (`VkRenderPass`, `VkFramebuffer`,
  compatibilité pipeline↔pass) et **garde l'information utile aux tuiles**
  (`loadOp`/`storeOp` inline) : les drivers mobiles récents tuilent avec. Godot, lui,
  garde les render passes : son plancher est le parc Android 2018 (drivers Vulkan 1.0)
  et son renderer Mobile exploite les subpasses — sa contrainte d'écosystème, pas la
  nôtre (Mobile dernier sur notre route, cap UI mono-passe, décision réversible car
  enfermée dans `HAL.Vulkan` sous `IRenderer`).
- **Premier triangle sans données** — positions et couleurs en dur dans le vertex
  shader (`gl_VertexIndex`) : zéro buffer, zéro mémoire GPU. On isole l'apprentissage
  du pipeline de celui de la mémoire (Bloc 4).
- **Shaders en GLSL brut, compilés au build** (`glslangValidator`, cible MSBuild,
  SPIR-V embarqué en ressource) — pas d'abstraction de shading : ses clients (langage
  custom à la Godot, übershader) appartiennent aux ères UI/2D, et on n'abstrait pas
  avant d'avoir écrit des shaders concrets. L'idée übershader UI d'Omar (un shader
  quad à champ « mode », ensemble fermé) est notée pour la brique UI.
- **Validation layers en debug** — `VK_LAYER_KHRONOS_validation` activée à la création
  d'instance si présente (build Debug uniquement) : les messages détaillés du driver
  sont le filet de sécurité de toute la phase.

### La chaîne de montage du pipeline (passe 1)

- **Cinq postes** : vertex shader (programmable, 1×/sommet, contrat : `gl_Position`)
  → assemblage de primitives (figé, piloté par la topologie) → rasterization (figée :
  couverture des pixels + **interpolation barycentrique** des sorties du VS — le
  dégradé naît ici, gratuit) → fragment shader (programmable, 1×/fragment, contrat :
  une couleur) → blending (configurable ; éteint pour un triangle opaque, se
  réveillera pour l'UI).
- **Le PSO fige tout d'avance** — l'anti-OpenGL : au lieu d'un état global mutable
  que le driver doit revalider/recompiler au draw (hitches), la configuration
  complète (shaders, topologie, rasterizer, blending, formats des attachments) est
  déclarée dans un objet immuable, compilé une fois en microcode réel. Draw = bind +
  go. Corollaire : un shader différent = un pipeline différent.
- **Deux soupapes** : le *dynamic state* — viewport/scissor fournis au draw, donc le
  pipeline **survit au resize** (seule la swapchain est recréée, comme aujourd'hui) ;
  les *specialization constants* (variantes de compilation), hors roadmap.
- **PSO du Bloc 3** : 2 `VkShaderModule`, aucun vertex input (sommets en dur),
  topologie triangle list, rasterizer par défaut, pas de blending, viewport/scissor
  dynamiques, format couleur de la swapchain déclaré au pipeline (le dynamic
  rendering n'ayant plus de render pass pour le porter).

### Shaders et SPIR-V (passe 2)

- **Les deux shaders du triangle sont écrits** — `triangle.vert` : tableaux constants
  indexés par `gl_VertexIndex` (le « triangle sans données »), sortie couleur ;
  `triangle.frag` : passthrough de la couleur interpolée. Le contrat entre étages
  passe par `layout(location = N)` — et entre les deux, le rasterizer interpole.
  NDC Vulkan **Y vers le bas**, encodé à la main dans les constantes (c'est le clip
  space que `Matrix4x4.CreateOrthographic` absorbe quand il y a une matrice).
- **SPIR-V = l'IL du GPU** — GLSL est le C#, SPIR-V est l'IL, le driver fait le JIT
  final vers le microcode. Compilation **hors ligne** par `glslangValidator -V` :
  les erreurs sortent au build chez nous, le driver ne reçoit que du binaire validé
  (fin des compilateurs GLSL par vendor et de leurs divergences, la plaie d'OpenGL).
- **Intégration au build** — sources versionnées dans `HAL.Vulkan/Shaders/`, cible
  MSBuild `glslangValidator` → `obj/`, `.spv` embarqués en **ressources** d'assembly
  (artefacts, jamais commités). Runtime : `GetManifestResourceStream` →
  `vkCreateShaderModule` (simple enveloppe d'octets ; le point d'entrée `main` est
  désigné par le pipeline, par étage). Assumé : builder `HAL.Vulkan` exige
  `glslangValidator` sur la machine.

### Le dynamic rendering en pratique (passe 3)

- **`VkImageView` : l'interprétation déclarée d'une image** — le pipeline ne dessine
  jamais sur une image brute, toujours à travers une view (format, aspect, mips,
  couches) ; un `Span<T>` typé posé sur la mémoire brute. Une view par image de
  swapchain — naissent avec elle, recréées au resize, détruites avec elle.
- **`vkCmdBeginRendering` = l'épisode déclaré inline** — `VkRenderingInfo` porte le
  renderArea et l'attachment couleur (`view`, `layout`, `loadOp`, `storeOp`,
  `clearValue`). **Le clear cesse d'être une commande** (`vkCmdClearColorImage`
  supprimé) **et devient une propriété d'ouverture** : `loadOp = Clear` — la forme
  que les GPU à tuiles savent rendre gratuite.
- **Les barriers restent, les destinations changent** — le dynamic rendering laisse
  les transitions de layout à notre charge (le render pass les faisait
  implicitement) : `Undefined → ColorAttachmentOptimal` (stage ColorAttachmentOutput,
  access ColorAttachmentWrite) avant l'épisode, `→ PresentSrcKhr` après.
- **`pNext` : le mécanisme d'extension universel** — chaque struct Vulkan peut
  chaîner des suppléments en liste. Deux branchements : `ApiVersion` déclaré → 1.3
  (la machine est en 1.4), et `VkPhysicalDeviceDynamicRenderingFeatures
  { dynamicRendering = true }` chaînée au `VkDeviceCreateInfo`. Au Bloc 3, le
  pipeline déclarera son format d'attachment via `VkPipelineRenderingCreateInfo`
  chaînée — le résidu du contrat render pass, réduit à un champ.

### La mémoire GPU (passe du Bloc 4)

- **La géographie d'abord** — VRAM (rapide pour le GPU) vs RAM système (atteinte via
  PCIe) ; Vulkan montre la carte au lieu de la cacher : des **heaps** (réservoirs
  physiques) et des **types** étiquetés `DEVICE_LOCAL` / `HOST_VISIBLE`
  (mappable par le CPU via `vkMapMemory`) / `HOST_COHERENT` (pas de flush manuel).
- **Itinéraire direct retenu** — buffer `HOST_VISIBLE | HOST_COHERENT`, mappé,
  copié, dessiné. Le **staging** (copie vers `DEVICE_LOCAL`) est différé avec son
  client : les vrais assets. Une allocation par buffer pour apprendre ; la pratique
  réelle (gros blocs sous-alloués, lib VMA, limite driver ~4096 allocations) est
  notée hors périmètre.
- **Buffer ≠ mémoire** — trois appels volontairement séparés : `vkCreateBuffer`
  (l'objet : taille + usage), `vkAllocateMemory` (le stockage, choisi par la boucle
  `FindMemoryType` croisant `vkGetBufferMemoryRequirements.memoryTypeBits` avec les
  propriétés voulues), `vkBindBufferMemory` (le mariage). La séparation permet la
  sous-allocation.
- **Mathematics touche le GPU** — vertex C# = `Vector2` position + `Vector3` couleur
  (stride 20, copie brute dans le buffer mappé — le payoff du layout vérifié de
  Mathematics). Le pipeline déclare le contrat : 1 binding description (stride 20,
  par sommet) + 2 attribute descriptions (loc 0 : `R32G32_SFLOAT` à 0 ; loc 1 :
  `R32G32B32_SFLOAT` à 8). Le shader perd ses tableaux et `gl_VertexIndex` ; il
  gagne `layout(location=N) in`. Nouvelle dépendance : HAL.Vulkan → Mathematics.

### Hors périmètre (volontairement)

Intégration SceneGraph↔renderer, quads UI, textures, uniforms/descripteurs,
profondeur, multi-frames in flight — chacun viendra avec son client.

## Blocs

- [x] **Bloc 0 — Outillage** ✅
  - Paquets installés : `glslang-tools`, `vulkan-validationlayers`, `vulkan-tools`
  - Machine vérifiée : RTX 3050 en Vulkan **1.4.329**, `VK_KHR_dynamic_rendering`
    présent sur tous les devices (Intel 1.4.318, llvmpipe inclus)
  - `VK_LAYER_KHRONOS_validation` activée en Debug si installée
    (`vkEnumerateInstanceLayerProperties` ajouté au P/Invoke)
  - Validation : layer chargée (trace loader), 14 tests d'intégration Vulkan verts,
    Sandbox 6 s sans aucun message — le chemin clear existant est propre

- [x] **Bloc 1 — Conception** ✅ (passes progressives)
  - [x] Passe 1 : la chaîne de montage ✅ → section « La chaîne de montage du
    pipeline » ci-dessus
  - [x] Passe 2 : shaders et SPIR-V ✅ → section « Shaders et SPIR-V » ci-dessus
  - [x] Passe 3 : dynamic rendering en pratique ✅ → section « Le dynamic rendering
    en pratique » ci-dessus

- [ ] **Bloc 2 — Image views + chemin rendering** — le clear actuel réécrit via
  `vkCmdBeginRendering` (loadOp = Clear) : écran toujours gris, mais par le chemin
  du dessin ; le chemin transfer disparaît

- [x] **Bloc 3 — Le triangle** ✅ — shaders GLSL versionnés + compilation MSBuild
  (`glslangValidator` → `.spv` embarqués en ressources, erreur GLSL = erreur de
  build), `VulkanPipeline.cs` (PSO complet, `VkPipelineRenderingCreateInfo` en
  `pNext`, modules détruits sitôt le pipeline compilé), bind + viewport/scissor
  dynamiques + `vkCmdDraw(3)` dans l'épisode. Triangle en dégradé confirmé au
  Sandbox, 14 tests verts, validation muette

- [x] **Bloc 4 — Vertex buffer** ✅ — `VulkanBuffers.CreateVertexBuffer<T>`
  (create → requirements → FindMemoryType → allocate → bind → map/copie/unmap,
  host-visible direct), `TriangleVertex` = `Vector2` + `Vector3` de Mathematics en
  copie brute (stride 20), vertex input déclaré au pipeline, shader sur
  `layout(location) in`. Triangle strictement identique confirmé au Sandbox,
  14 tests verts, validation muette. Dépendance nouvelle : HAL.Vulkan → Mathematics

---

## Résultat

Le moteur sait dessiner : pipeline graphique complet en dynamic rendering (Vulkan 1.3,
sans render pass), shaders GLSL versionnés et compilés en SPIR-V au build (embarqués
en ressources), PSO avec viewport/scissor dynamiques (survit au resize), vertex
buffer en mémoire host-visible nourri par les types Mathematics, validation layer
active en Debug et muette sur tout le chemin. Prochaines briques (hors roadmap) :
l'intégration SceneGraph↔renderer et la brique UI (quads, layout, bindings) — les
deux sont maintenant débloquées ; voir le cap produit dans CLAUDE.md.

*Tâche terminée*