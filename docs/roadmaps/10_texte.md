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

## Blocs

- [ ] **Bloc 1 — Conception** — passe 1 (l'arc + hautes décisions) ✅ ci-dessus.
  Le design fin de chaque bloc suivant se fait à son ouverture (passes
  progressives), comme en roadmap 09.

- [ ] **Bloc 2 — Les textures dans le renderer** — l'infra GPU manquante :
  sampled image + image view + sampler, descriptor set layout + pool + sets,
  upload par **staging buffer + device-local** et transitions de layout, mode
  texture de l'übershader (UV déjà émis). Contrat HAL : `ITexture` +
  `DrawTexturedQuad`. *Sandbox : une image statique sur un quad.*

- [ ] **Bloc 3 — Les glyphes** — FreeType en P/Invoke (chargement de police,
  `FT_Load_Glyph`, bitmap + métriques), atlas de glyphes (packing, cache par
  codepoint × taille). *Sandbox : dump de l'atlas / quelques glyphes posés.*

- [ ] **Bloc 4 — La mise en forme** — layout `string → glyphes positionnés`
  (avance, kerning, retour à la ligne), nœud `Label`/`TextNode` qui se **mesure**
  et nourrit le layout réactif, texte/couleur/taille réactifs. *Sandbox : un
  label réactif dans l'UI.*

- [ ] **Bloc 5 — Le champ texte** — édition (caret, insertion/suppression via
  `TextInput`), navigation clavier + **répétition de touche**, binding
  bidirectionnel `Reactive<string>`. Le vrai client. *Sandbox : on tape dans un
  champ lié à un label.*

*Tâche en cours : bloc 1 (conception), passe 1 faite.*