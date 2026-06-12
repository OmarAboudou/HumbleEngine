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

- [ ] **Bloc 1 — Conception** (passes progressives)
  - [ ] Passe 1 : la chaîne de montage du pipeline — étages programmables vs fixes,
    pourquoi le PSO fige tout d'avance
  - [ ] Passe 2 : shaders et SPIR-V — GLSL, compilation au build, `gl_VertexIndex`,
    interpolation des sorties entre sommets
  - [ ] Passe 3 : dynamic rendering en pratique — image views, attachments,
    branchement sur les barriers existantes

- [ ] **Bloc 2 — Image views + chemin rendering** — le clear actuel réécrit via
  `vkCmdBeginRendering` (loadOp = Clear) : écran toujours gris, mais par le chemin
  du dessin ; le chemin transfer disparaît

- [ ] **Bloc 3 — Le triangle** — deux shaders, `VkShaderModule`, pipeline,
  `vkCmdDraw(3)` : triangle coloré (interpolation) vérifié au Sandbox + test
  d'intégration (cycle de frame avec pipeline, validation muette)

- [ ] **Bloc 4 — Vertex buffer** (avec sa passe de conception : heaps, types
  mémoire, host-visible vs device-local, vertex input) — le même triangle, alimenté
  par des données C#