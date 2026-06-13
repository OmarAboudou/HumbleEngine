# Roadmap — Le texte (glyphes → champ éditable)

Objectif : le moteur sait afficher du texte, puis l'éditer — le **vrai client du
two-way binding** (`Reactive<string>` ↔ champ texte). Le chemin va du métal vers
l'UI : d'abord les **textures** dans le renderer (l'infra GPU absente), puis les
**glyphes** (police → atlas), puis la **mise en forme** (un `Label` qui se
mesure), enfin le **champ texte** (caret, édition, navigation). Le canal
`TextInput` né à la roadmap 09 attendait précisément ce client.

Découpage en blocs — chaque bloc compile, **se voit** (Sandbox) et est validé avant le suivant.

## Décisions de conception

### L'arc et les hautes décisions (passe 1 — validé 2026-06-13)

- **Du métal vers l'UI, en quatre étages** (proposé en conception, validé) : on
  voit une **texture avant le moindre glyphe** (bloc 2), des **glyphes avant la
  moindre ligne de texte** (bloc 3), un **label avant tout champ** (bloc 4),
  l'édition en dernier (bloc 5). Chaque étage découple un risque : l'infra GPU
  (descriptors, sampler, staging, transitions de layout) ne se mêle pas des
  polices ; la rastérisation ne se mêle pas de la mise en forme ; la mise en
  forme ne se mêle pas de l'édition.
- **Rastérisation : FreeType en P/Invoke** (validé 2026-06-13 — fork tranché par
  Omar) : `libfreetype.so` au même titre que `libX11`, `libxkbcommon`,
  `libvulkan`, `libdecor` déjà dans le moteur — **cohérent avec le pattern
  existant** (« P/Invoke plutôt que wrappers »). On apprend l'intégration
  pertinente au moteur (`FT_Load_Glyph`, bitmap, métriques, kerning, atlas, le
  côté GPU) sans le détour scan-conversion d'un rastériseur maison (un projet de
  rendu de police à lui seul, orthogonal au moteur). Un port NuGet (stb) écarté :
  c'est le wrapper que l'éthos refuse.
- **Le texte est un quad texturé** (recommandé en conception, à confirmer au
  bloc 2) : contrat HAL générique **`ITexture` + `DrawTexturedQuad(rect,
  texture, uvSubRect, tint)`**, et le texte n'est « qu'un » quad sur le sous-rect
  du glyphe dans l'atlas — **les images viennent gratuites**, et le `mode`
  texture de l'übershader (UV déjà émis) sert les deux. La granularité (un draw
  par glyphe) et son batching restent l'optimisation différée déjà notée
  (centaines de quads).
- **Portée mise en forme : Latin LTR d'abord** (recommandé) : `string` →
  glyphes positionnés, avance horizontale + kerning, retour à la ligne simple.
  **Différés** : HarfBuzz (scripts complexes, ligatures), bidi, IME/composition
  (déjà différé en 09 avec l'XIM), sélection multi-ligne riche.
- **Police : un `.ttf` livré dans le repo** (recommandé) : déterministe, pas de
  dépendance à fontconfig (découverte des polices système) — différée avec son
  client (l'éditeur, le choix de police utilisateur).
- **Le champ texte est le client du two-way binding** : il consomme les
  `TextInput` (canal HAL déjà là), porte un caret, navigue au clavier — et c'est
  lui qui réclame la **répétition de touche** (Backspace maintenu), notée dès la
  roadmap 09 comme cliente de cette phase (le compositeur Wayland n'envoie que
  `repeat_info`, la répétition est à la charge du client).

### Les textures dans le renderer (bloc 2 — validé 2026-06-13)

- **Contrat HAL** : `ITexture : IDisposable` + `IRenderer.CreateTexture(
  ReadOnlySpan<byte> pixels, int width, int height, TextureFormat format)` +
  `DrawTexturedQuad(Rect rect, ITexture texture, Rect uvSubRect, Vector4 tint)`.
  Le texte sera un quad sur le `uvSubRect` du glyphe dans l'atlas ; les images
  arrivent gratuites. `VulkanTexture` porte image + mémoire + view + sampler +
  descriptor set, avec la **garde d'origine** déjà éprouvée (`VulkanMesh.Owner` :
  une texture étrangère rejetée plutôt qu'un crash latent).
- **Format** : `RGBA8` au bloc 2 (l'image du Sandbox), `R8` ajouté au bloc 3
  (la couverture alpha 8 bits que sort FreeType). `TextureFormat` enum dès
  maintenant pour ne pas réécrire le contrat.
- **Upload : staging + device-local** (validé 2026-06-13 — recommandé, anticipé
  par les notes 07-08 « staging buffer + device-local ») : buffer host-visible
  → `vkCmdCopyBufferToImage` → image device-local en tiling optimal, transitions
  `Undefined → TransferDst → ShaderReadOnly`, sur un **command buffer one-shot
  soumis et attendu** à la création. Écarté : l'image host-visible linéaire
  (plus simple, cohérente avec le chemin mesh actuel, mais format restreint et
  plus lente — il faudrait migrer pour les vrais assets de toute façon).
- **Un seul pipeline, texture blanche 1×1 par défaut** (validé 2026-06-13 —
  recommandé) : ajouter un `set 0, binding 0 = combined image sampler`
  (fragment) au layout du pipeline quad veut dire que les draws flat (`mode 0`)
  ont un layout déclarant un sampler ; plutôt que **scinder l'übershader en deux
  pipelines** (ce qui briserait « un pipeline pour toute l'UI » et rendrait un
  `BindPipeline` entre flat et texturé), les draws flat bindent le set d'une
  texture blanche 1×1 — « flat = blanc × couleur », un set valide toujours
  bindé. Modes übershader : `0` flat, `1` texture (`texture(tex,uv) × tint`),
  `2` glyphe (`.r` comme couverture, bloc 3).
- **Descriptor set par texture** : alloué d'un pool du renderer à `CreateTexture`,
  libéré au `Dispose`. Bindless / descriptor indexing différé (son client : des
  centaines de textures distinctes, pas l'UI d'aujourd'hui où l'atlas est *une*
  texture bindée une fois).

### Les glyphes (bloc 3 — validé 2026-06-13)

- **Un nouveau projet `HumbleEngine.Text`** (validé 2026-06-13 — recommandé) :
  isole la P/Invoke FreeType + `Font` + `GlyphAtlas`, namespace `HumbleEngine`
  (API publique), dépend de `HAL` (pour `ITexture`/`IRenderer`) + `Mathematics`.
  Miroir de l'isolation `HAL.Vulkan`/`HAL.X11` — la rastérisation bas-niveau ne
  se mêle pas aux nœuds de scène. `SceneGraph` y référera au bloc 4 (le `Label`).
- **Rastérisation FreeType en P/Invoke** : `FT_Init_FreeType`,
  `FT_New_Memory_Face` (depuis le `byte[]` embarqué, pas de chemin),
  `FT_Set_Pixel_Sizes`, `FT_Load_Char(…, FT_LOAD_RENDER)` → bitmap +
  métriques (bearing, advance). Les gros structs C (`FT_FaceRec`,
  `FT_GlyphSlotRec`, `FT_Bitmap`, `FT_Glyph_Metrics`) mappés par offsets — le
  morceau d'apprentissage.
- **Atlas pré-cuit** (choisi 2026-06-13 — délégué par Omar, reco retenue) : au
  chargement, rastériser une fois un **charset fixe** (ASCII 32–126 + supplément
  Latin-1 : les accents français déjà tapés au bloc 5), packer en un bitmap
  **R8** CPU (shelf-packing, atlas de taille fixe), créer **une texture
  immuable** via le `CreateTexture` du bloc 2 (réutilisé tel quel, zéro nouveau
  contrat HAL). **Différé : l'atlas dynamique** (texture vierge, glyphes
  uploadés à la demande via un futur `ITexture.Update(region)` + copie staging
  sous-rect) — son client : les grands charsets / CJK.
- **L'enregistrement par glyphe** : `uvSubRect` (0..1 dans l'atlas) + taille px
  + bearing (left/top) + advance — exactement ce que consommera le layout du
  bloc 4. Récupérable par codepoint (cache du charset cuit).
- **Police embarquée : `DejaVuSans.ttf`** (validé — reco) en `EmbeddedResource`,
  miroir exact de l'embed des shaders SPIR-V ; déterministe, pas de fontconfig.
  Licence permissive (DejaVu). Découverte des polices système différée.
- **Démo Sandbox** : quelques glyphes posés à la main (`DrawTexturedQuad` sur
  leurs sous-rects, `mode 2`) — du texte visible avant le moindre `Label`.

## Blocs

- [ ] **Bloc 1 — Conception** — passe 1 (l'arc + hautes décisions) ✅ ci-dessus.
  Le design fin de chaque bloc suivant se fait à son ouverture (passes
  progressives), comme en roadmap 09.

- [x] **Bloc 2 — Les textures dans le renderer** ✅ — l'infra GPU posée.
  `VulkanNative` : la fondation P/Invoke (image/sampler/descriptor/copy + structs
  miroirs). Übershader : `sampler2D` set 0/binding 0 + modes `1` texture, `2`
  glyphe (prêt bloc 3) ; `uvRect` ajouté aux push constants (bloc per-draw 48→64,
  l'UV interpolée dans le sous-rect). Contrat HAL : `ITexture` (Width/Height) +
  `TextureFormat` (Rgba8/R8) + `CreateTexture`/`DrawTexturedQuad`, OpenGL en
  `NotSupported`. `VulkanTexture` : image device-local + view + descriptor set +
  garde `Owner` (miroir `VulkanMesh`). `VulkanRenderer` : set layout (créé avant
  le pipeline quad qui le reçoit), pool (sets libérables, plafond 256), sampler
  partagé (linéaire, clamp), **texture blanche 1×1 par défaut** (les draws flat la
  bindent → set toujours valide, un seul pipeline). `CreateTexture` : image
  optimal-tiling + **staging one-shot** (`vkQueueWaitIdle`, transitions Undefined
  → TransferDst → ShaderReadOnly). `_boundDescriptorSet` (skip des binds
  redondants, comme `_boundPipeline`). *Sandbox : `ImageNode` (damier RGBA généré
  au code) — validé interactivement.* Tests : 3 d'intégration Vulkan
  (création+draw, format R8, garde d'origine) ; 219 unitaires + 53 intégration
  verts, 0 erreur de validation Vulkan sur un run du Sandbox.

- [x] **Bloc 3 — Les glyphes** ✅ — nouveau projet **`HumbleEngine.Text`**
  (namespace `HumbleEngine`, deps HAL + Mathematics), DejaVuSans.ttf embarqué.
  `FreeTypeNative` (P/Invoke `libfreetype.so.6` + structs `FtBitmap`/`FtGlyphSlot`
  **mappés par offsets 64-bit** : advance@128, bitmap@152, bearing@192/196 —
  validés par les tests). `Font` (lib+face+bytes épinglés, `Rasterize` copie en
  managé via `RasterizedGlyph`, `Font.Default(px)`). `GlyphAtlas` pré-cuit
  (charset ASCII + Latin-1, shelf-packing R8 512², texture immuable du bloc 2,
  `char → Glyph`). `Glyph` record (uvSubRect + taille + bearing + advance).
  **Primitive HAL `DrawGlyph`** (mode 2 : couverture `.r` × couleur), câblée
  Vulkan/OpenGL/Fake — la conception disait « DrawTexturedQuad mode 2 », un draw
  dédié est plus net. *Sandbox : `TextNode` pose une chaîne (ASCII + accents) à
  la main — validé interactivement.* Tests : 5 unitaires (sans display, via
  `FakeRenderer` — 'A' à métriques saines = preuve des offsets, espace = advance
  sans bitmap, accent dans le charset, hors-charset absent, atlas R8 512²).
  224 unitaires + 53 intégration verts, 0 erreur de validation sur un run.

- [ ] **Bloc 4 — La mise en forme** — layout `string → glyphes positionnés`
  (avance, kerning, retour à la ligne), nœud `Label`/`TextNode` qui se **mesure**
  et nourrit le layout réactif, texte/couleur/taille réactifs. *Sandbox : un
  label réactif dans l'UI.*

- [ ] **Bloc 5 — Le champ texte** — édition (caret, insertion/suppression via
  `TextInput`), navigation clavier + **répétition de touche**, binding
  bidirectionnel `Reactive<string>`. Le vrai client. *Sandbox : on tape dans un
  champ lié à un label.*

*Tâche en cours : bloc 3 ✅ ; prochain — bloc 4 (la mise en forme : layout `string → glyphes`, `Label` qui se mesure).*