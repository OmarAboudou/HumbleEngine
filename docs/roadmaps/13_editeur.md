# Roadmap — L'éditeur (dogfooding)

> **EN COURS (2026-06-13)** — première phase de l'ère éditeur. Le moteur sert
> d'abord à faire des applications UI ; ici il commence à se servir lui-même.

## Cadrage (tranché avec Omar le 2026-06-13)

- **Topologie : mono-fenêtre, panneaux dockés** (modèle Godot/Unity par défaut).
  Les panneaux sont des nœuds UI internes, pas des fenêtres OS. Conséquence : le
  **découplage pompe/rendu (roadmap 11) reste différé** — on ne paie pas cette
  refonte lourde pour avoir un éditeur debout. On la rouvrira si/quand on voudra
  des panneaux flottants (vraies fenêtres détachables).
- **MVP : un éditeur de scène/UI, construit par le bas.** On part de la fondation
  (les collections réactives, dette laissée par les signals) et on remonte vers le
  visible : panneau hiérarchie → inspecteur → viewport. Chaque bloc compile, **se
  voit** dans le Sandbox, et est validé avant le suivant.

## Les blocs

### Bloc 1 — Les collections réactives ✅ (le rebranchement du `LinearContainer`)

**Le problème.** L'auto-tracking des signals (`Tracking`/`Computation`/
`IReactiveSource`) ne marche que sur les cellules : `Property.Value` /
`Computed.Value` trackent en lecture et invalident en écriture. Les collections
(`ObservableList<T>`, `NodeList<T>`) sont observables *autrement* — par narration
exacte (`Added`/`Removed` + index) — et ne participent pas à l'auto-tracking.
Résultat : le `LinearContainer` se recâble **à la main** (`Children.Added += …`,
`child.Size.Changed += …` abonné/désabonné par enfant, `Spacing.Changed += …`),
exactement le bookkeeping que l'auto-tracking est censé supprimer.

**Le design : un *signal de structure* par collection.**
- Une collection observable expose **une source de structure unique**
  (`IReactiveSource`). Granularité = **structure entière**, pas un signal par
  élément (le layout relit toute la liste de toute façon).
- Toute lecture qui dépend de la composition (`Count`, indexeur, `GetEnumerator`)
  appelle `Track` ; toute mutation structurelle (`Added`/`Removed`) invalide les
  computations — **en plus** de la narration exacte, qui reste pour `BindItemsFrom`
  et la décoration incrémentale. Les deux mécanismes coexistent (clients
  différents : « quoi/où » vs « la composition a changé, relis »).
- `replace` (indexeur d'`ObservableList`) = `Removed` puis `Added` → invalide 2×,
  état intermédiaire transitoire assumé (philosophie « eager, transients OK » du
  reste du graphe).

**La factorisation.** Le pattern `observers + AddObserver/RemoveObserver +
NotifyObservers`, dupliqué dans `Property` et `Computed`, est extrait dans un type
interne partagé `SourceObservers : IReactiveSource` (il *est* la source — `Track`
s'enregistre auprès de lui, `NotifyChanged` re-run ses observateurs). Composé par
les **quatre** : `Property`, `Computed`, `ObservableList`, `NodeList`.

**Le payoff.** `LinearContainer.Relayout` devient un `CreateEffect` : lire
`Children.Count`/l'indexeur track la structure, lire `child.Size.Value` track
chaque enfant, lire `Spacing.Value` track le spacing. Tout le câblage manuel
(`Added`/`Removed`/`Size.Changed`/`Spacing.Changed`, abonnement par enfant)
**disparaît** — l'effet se réabonne aux enfants présents à chaque run, le va-et-vient
est gratuit. Pas de feedback : l'effet écrit `Position`, qu'il ne lit jamais.

**Tests** (NUnit, style contraintes) : un `Effect` se re-run sur Add/Remove/replace
d'une `ObservableList` et d'une `NodeList` ; le layout d'un `Column`/`Row` se met à
jour à l'ajout/retrait/redimensionnement d'un enfant sans abonnement manuel ;
disposer le nœud arrête l'effet.

### Bloc 2 — Panneau hiérarchie + sélection

Premier vrai client des collections réactives : un panneau qui **liste l'arbre réel**
et se met à jour live, où **cliquer sélectionne** un nœud (état réactif que
l'inspecteur consommera au bloc 3).

**Décision de doctrine (tranchée avec Omar le 2026-06-13).** La règle gravée
« closed by default / scenes never expose » devient **« fermé à l'écriture, ouvert à
la lecture »**. L'éditeur est un cas d'usage réel d'introspection (debug : dérouler
l'intérieur d'une scène pour voir l'état réel de ses nœuds). On abandonne
l'*inatteignabilité* de l'intérieur ; on garde l'*intégrité* : lecture seule garantie
par le type, l'édition continue de passer par la surface voulue (les slots). Bénin car
aucun invariant runtime n'est en jeu en lecture, et cohérent avec la philosophie
hackable ([[feedback-api-visibility]]).

Découpage en sous-blocs — chacun compile, **se voit** (Sandbox), validé avant le suivant :

- **2a — La couche base devient une source observable. ✅** `Node._children` n'a
  aujourd'hui qu'un broadcast de *départ* (`ChildDeparted`, interne) ; on lui ajoute le
  *départ + l'ajout* et un **signal de structure** (un `SourceObservers`, comme au bloc
  1). On expose `Node.Children` en **`public ReadOnlyNodeList<Node>`** (au lieu de
  `protected IReadOnlyList<Node>`) — vue **observable, lecture seule garantie par le
  type** (non-castable, l'esprit de `ReadOnlyProperty`). Nouvelle classe
  `ReadOnlyNodeList<Node> : IReadOnlyObservableList<Node>`. Impact vérifié : rendu,
  hit-test et hooks passent par `_children` privé, seul `LinearContainer` lit `Children`
  (via son propre type masquant) → rien ne casse. Doc de `Node`/`Scene` mise à jour
  (nouvelle doctrine). *L'arbre réel devient observable de bout en bout — fondation
  durable au-delà de l'éditeur.*
- **2b — L'état d'édition : la sélection. ✅** Un `Property<Node?> Selection` porté par un
  petit contexte d'édition (`EditorState`), injecté dans les panneaux. Réactif : le
  bloc 3 s'y branchera sans couplage.
- **2c — Le panneau hiérarchie (la vue). ✅** (`HierarchyView` + `HierarchyRow`,
  `BindItemsFrom` récursif sur `Node.Children`, live. **Découverte** : les conteneurs ne
  s'auto-mesurent pas — le *content-sizing* est une extension différée — donc `HierarchyRow`
  fait son propre layout + calcule sa `Size` ; le sizing-par-contenu des `LinearContainer`
  reste un bloc futur.) Un nœud UI récursif : une ligne par nœud
  (Label = `Name` ou type), une sous-liste indentée bindée par `BindItemsFrom` sur
  `node.Children`. Live gratuit via le signal de structure du 2a. Pas de pliage
  (différé).
- **2d — Sélection au clic. ✅** (clic → `Selection.Value`, surbrillance du fond qui suit
  la sélection. **Découverte** : conflit clic-nœud vs sélection-texte → ajout du flag
  `UINode.Hittable` — transparent au hit-test, le `IGNORE` de Godot `mouse_filter` ; le
  label de ligne est `Hittable=false`, le clic atteint la ligne. Pas de texte
  sélectionnable dans la hiérarchie, comme un bouton Godot.) Une ligne écrit `Selection.Value` sur clic (input déjà
  routé : `RouteInput`/`OnInput`/hit-test/bubbling). Retour visuel : le fond de la ligne
  suit `Selection == ce nœud` (binding/`Computed`).
- **2e — Tests + démo Sandbox. ✅** (code prêt ; validation visuelle par Omar — pas de
  display côté assistant.) `HierarchyDemoScene` (Sandbox) = un arbre exemple détaché
  introspecté par un `HierarchyView` ; `Program.cs` accepte `--demo sandbox|hierarchy`
  (seule la démo Sandbox s'anime). `dotnet run --project HumbleEngine.Sandbox -- --demo hierarchy`.
  Tests unitaires aux blocs 2a-2d (sélection/binding/Hittable ; input non instrumentable en intégration).

**Bloc 2 terminé** (démo validée par Omar le 2026-06-13). Prochain : bloc 3
(inspecteur) — voir `project-reactive-axes` (mémoire) pour le supertype `IObservableValue`.

Question de structure ouverte : créer un projet **`HumbleEngine.Editor`** (panneau +
`EditorState`) dès maintenant, ou itérer d'abord dans le Sandbox ? Recommandation :
créer le projet (c'est la phase, et les blocs 3-4 s'y poseront).

Suivi noté : alignement cellule/liste — `Property` a un wrapper-classe lecture seule
(`ReadOnlyProperty`), les listes n'ont qu'une interface. Un `ReadOnlyObservableList` /
`AsReadOnly()` sur les listes serait cohérent, mais décision propre, hors bloc 2.

### Bloc 3 — Inspecteur ✅ (2026-06-14)

**Fondation (3a) — `IObservableValue` dans Reactive.**
Nouvelle interface racine `IObservableValue` (`ValueType`, `object? Value` auto-trackée)
+ `IObservableValue<out T> : IObservableValue` (valeur typée, covariante). `IReadOnlyProperty<T>`
étend désormais `IObservableValue<T>` — `Property<T>`, `Computed<T>`, `ReadOnlyProperty<T>` et
`NodeSlot<TChild>` implémentent explicitement la couche non générique. Garantie clé : un `Effect`
lisant `IObservableValue.Value` s'abonne correctement à la source, même via l'interface non générique.

**Découverte de propriétés — `NodeInspector` (static).**
Reflexion par type, mise en cache par type concret, parcours base → dérivé (ordre visuel
naturel). Convention implicite : `public` + `IObservableValue` = inspectable, sans attribut.

**Widget par type — `InspectorRow`.**
Factory interne choisit le widget selon `ValueType` et la writabilité :
- `Property<string>` → `TextField` two-way binding
- `Property<bool>` → `BoolToggleWidget` (couleur + clic)
- `Property<float>` → `TextField` avec parse/format (Changed → display unidirectionnel ; display → source au keystroke)
- tout autre `IObservableValue` → `Label` réactif (`Effect` sur `.Value` non typé)

**Panneau — `InspectorView`.**
Écoute `Selection.Value` via `CreateEffect` (auto-tracking). Sur changement : dispose les
rows précédentes (via une `List<UINode>` privée `_activeRows`, sans Track parasite), recrée
via `NodeInspector`. Fond + séparateur + header + colonne de rows. `SyncLayout` réactif via
second `Effect`.

**Démo Sandbox (démo `--demo hierarchy`)** mise à jour : hiérarchie à gauche (220 px),
inspecteur à droite (300 px). Cliquer un nœud → l'inspecteur montre ses propriétés live.

**Tests (9 tests) :** discovery reflexion (noms, ordre base/dérivé, valeurs live, exclusions) ;
`InspectorView` démarre vide, crée N rows, vide sur `null`, reconstruit au changement de
sélection, dispose les rows obsolètes.

**Note architecture :** la Convention implicite "public + IObservableValue = inspectable"
peut s'enrichir d'un `[HideInInspector]` au cas par cas, sans refonte (idiome [[feedback-api-visibility]]).
Le sizing-par-contenu des `LinearContainer` reste différé (contourné dans `InspectorRow` comme dans `HierarchyRow`).

### Bloc 4 — Viewport ✅ (2026-06-14)

**`ViewportNode` dans SceneGraph.**
`UINode` avec `NodeSlot<Node> _content` (exposé en `Content`) et
`Property<Vector4> Background`. `OnDraw` peint le fond ; la traversée normale rend le
sous-arbre — aucun ajout d'API renderer, aucun décalage manuel : le `GlobalRect` des nœuds
du sous-arbre accumule naturellement la position du viewport. Convention : pas de scissor sur
cet étage (les enfants peuvent déborder, comme dans les autres conteneurs) — le clipping
s'ajoutera comme capacité renderer quand un cas concret l'exigera.

**Démo mise à jour** (`--demo hierarchy`).
Trois panneaux côte à côte (1100 × 700) :
- Hiérarchie (220 px) — arbre live, clic → sélection.
- Inspecteur (300 px) — propriétés du nœud sélectionné, éditables.
- Viewport (380 px) — rendu live du sample : la scène échantillon est le `Content` du
  `ViewportNode`, elle entre dans l'arbre vivant, les `Label`/`TextField` s'y mesurent via
  `OnAttached`, les `Column`/`Row` se layoutent réactivement.

**Tests (8 tests) :** `Content` null par défaut ; adopte l'enfant ; null détache ; deuxième remplace
le premier ; `GlobalRect` de l'enfant est décalé par la position du viewport ; `Background`
observable, non transparent par défaut, modifiable.

**Note architecture :** le chemin vers `WindowNode` reste inchangé : `VisualNode.Renderer` résoudra
un jour le plus proche ancêtre viewport au lieu du renderer de la SceneTree, sans toucher aucun
nœud — les nœuds sont déjà abstraits par rapport à leur source de renderer.
