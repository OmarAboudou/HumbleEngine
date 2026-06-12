# Roadmap — Bindings réactifs (HumbleEngine.Reactive)

Objectif : concevoir et implémenter la brique de réactivité du moteur — la cellule
observable (`Reactive<T>`), les bindings scalaires (`BindFrom`), la liste
observable (`ReactiveList<T>`) et le mapping réactif `BindItems(source, fabrique)`
avec son registre item→nœud (modèle `ItemContainerGenerator` de WPF/Avalonia).
C'est la brique qui porte le principe « les mises à jour passent exclusivement par
les bindings » du SceneGraph (principe 4 de `04_scenegraph.md`) — l'UI déclarée
one-shot, puis *éditée* par les bindings, jamais redécrite.

Découpage en blocs — chaque bloc compile, est testé et validé avant de passer au suivant.

## Décisions de conception

- **Assembly `HumbleEngine.Reactive`** — autonome, modèle Mathematics : tout le monde
  en aura besoin, elle n'a besoin de personne. Namespace racine `HumbleEngine`
  (API publique, utilisée partout dans le code applicatif).
- **Nommage `Reactive*`** (`Reactive<T>`, `ReactiveList<T>`) — en .NET,
  « Observable » désigne déjà deux autres choses : `IObservable<T>` (Rx — un flux
  d'événements *sans valeur courante*) et `ObservableCollection<T>` / l'attribut
  `[ObservableProperty]` du MVVM Toolkit. Notre type est une *cellule* — valeur
  courante + notification — dont le nom établi dans l'écosystème est
  `ReactiveProperty` (UniRx, lib ReactiveProperty), raccourci ici en `Reactive<T>`.
  Cohérent avec le nom de l'assembly.
- **Contrainte de convergence (héritée de la phase SceneGraph)** — `NodeSlot<T>` /
  `NodeList<T>` sont des « observables + sémantique d'arbre » : la brique devra soit
  les rendre observables (événements d'ajout/retrait), soit les réécrire sur les
  primitives réactives. Cas client : une scène qui décore chaque enfant injecté à
  l'insertion et défait la décoration au retrait = `BindItems`, pas un rebuild.
- **Durée de vie des bindings liée à la propriété par l'arbre** (décision actée en
  phase SceneGraph) — le mécanisme précis (qui se désabonne, quand) est l'objet de
  la passe 4 : un abonnement oublié est *la* fuite mémoire classique de WPF.
### Panorama des modèles réactifs (décidé en passe 1)

Position de la brique : **la cellule des signals, la déclaration explicite et typée de C#,
le push de Rx, le générateur d'items de WPF, et la paranoïa de WPF sur les durées de vie.**

- **WPF/Avalonia** — on prend : le concept de binding (« reste », déclaré une fois),
  le modèle `ItemContainerGenerator` (→ `BindItems`), la leçon des fuites mémoire
  (un binding est un abonnement ; les *weak events* de WPF sont un correctif après coup).
  On rejette : chemins en string + réflexion (viole le principe 3 — le système de types
  C# est le seul registre) et `DataContext` implicite (lookup ambiant — viole le principe 2).
- **Rx** — on prend : le modèle **push** et la transformation portée par le lien
  (`Select` ≈ `BindFrom(source, transform)`). On rejette : le *flux* comme abstraction
  centrale — un `IObservable<T>` n'a pas de valeur courante, or l'UI doit pouvoir lire
  « maintenant » ; l'algèbre d'opérateurs complète est hors besoin. (`ReactiveProperty`
  d'UniRx = « une cellule par-dessus Rx » — on garde le concept sans Rx dessous.)
- **Signals (SolidJS/Vue/TC39)** — on prend : le **modèle mental de la cellule** et la
  mise à jour fine, directement aux consommateurs (principe 4). On rejette : le tracking
  automatique des dépendances (contexte global implicite, magique à déboguer, hostile au
  multi-thread) — chez nous les dépendances sont explicites dans `BindFrom`.
- **Godot (contre-exemple)** — des signaux ≠ un système de binding : toute la
  synchronisation s'écrit à la main (« fais », jamais « reste »). Binding =
  événement **+ relation déclarée + cycle de vie géré**.

### Conception de `Reactive<T>` et des bindings scalaires (décidée en passe 2)

- **`Reactive<T>` = la cellule du tableur** — valeur courante (`Value` get/set) +
  notification (`Changed`, nouvelle valeur). `IReadOnlyReactive<T>` (Value get + Changed)
  pour les contrats de scène — c'est aussi le type des *sources* de binding, qui ne sont
  que lues.
- **Push synchrone, mono-thread** — notification immédiate sur place, même contrat
  mono-thread que le SceneGraph ; pas de scheduler ni de verrous en v1.
- **Égalité avant notification** (`EqualityComparer<T>.Default`) — écrire la même valeur
  ne notifie pas : pas de repaint inutile, et les convergences s'arrêtent d'elles-mêmes.
- **Une formule par cellule** — `BindFrom(source, transform)` : au plus un binding par
  cellule *cible* ; re-binder remplace ; le bind pousse immédiatement la valeur courante
  de la source (comme taper une formule dans Excel). Être *source* est illimité —
  toute cellule, liée ou non, reste écoutable par n tiers.
- **One-way strict, cycles interdits au bind** — `BindFrom` mutuel interdit : au bind,
  remontée de la chaîne des sources et exception claire si la cible y figure (chaque
  cellule ayant au plus une formule à une source, c'est un parcours de liste chaînée).
  Garde de réentrance à l'exécution en complément, pour les boucles créées par un
  handler `Changed` qui réécrit en amont.
- **Fail-fast sur la cible d'un one-way** — `Value = x` sur une cellule liée lève une
  exception (l'écrasement silencieux de WPF est *le* bug classique) ; `Unbind()` reprend
  la main explicitement. Une cellule libre s'écrit normalement.
- **`BindTwoWayFrom(other, aToB, bToA)`** — le bidirectionnel est un contrat *distinct*
  et explicite, jamais deux `BindFrom` croisés. Écho supprimé : un changement fait
  exactement un aller, jamais de retour — la terminaison ne dépend pas du fait que les
  transformations soient des inverses parfaits. Le `From` répond à « qui a raison au
  bind » : l'argument est la source de vérité initiale (sync immédiate), ensuite la
  relation est symétrique. Occupe le slot de binding des **deux** cellules (aucune ne
  peut avoir d'autre binding ; pas de chaînes de two-way). L'écriture manuelle reste
  permise aux deux bouts — c'est sa raison d'être (la frappe utilisateur entre par là).
- **Diamants : glitch accepté en v1** — `D` dérivé de `B` et `C` eux-mêmes dérivés de
  `A` peut être recalculé deux fois, dont une avec un état mixte. Transformations
  **pures** exigées (contrat documenté) → l'état final est toujours correct. Option
  scheduler topologique notée pour plus tard si un client souffre.
- **Multi-sources différé** — `BindFrom(s1, s2, (a, b) => …)` est purement additif
  (surcharges, N abonnements, détection de cycle sur un arbre au lieu d'une chaîne) ;
  ajouté quand un client réel le demandera.

### Conception de `ReactiveList<T>` (décidée en passe 3)

- **Type dédié — pas `Reactive<List<T>>`** — une mutation en place ne change pas la
  référence (l'égalité dirait « rien de neuf »), et « la liste a changé quelque part »
  ne permet que le rebuild. La liste **raconte** ses mutations : « ajouté X à l'index 2 ».
- **Pas de `Reset`, jamais** — l'échappatoire d'`INotifyCollectionChanged` force chaque
  consommateur à implémenter un chemin de reconstruction complet *en plus* de
  l'incrémental (et le `Clear()` de WPF émet un `Reset`…). Chaque mutation se raconte
  exactement.
- **Grain minimal : deux événements** — `Added(index, item)` / `Removed(index, item)`
  (l'item voyage dans l'événement — pour `Removed` il n'est plus lisible dans la liste).
  `Clear()` = N `Removed` de la fin vers le début (index stables pendant le démontage) ;
  `list[i] = x` = `Removed(i, ancien)` puis `Added(i, nouveau)` — état cohérent entre
  les deux, et sémantiquement juste pour `BindItems` (autre item = autre nœud).
- **`Move` différé, les yeux ouverts** — son client réel (tri, drag-drop) arrive avec la
  brique UI ; sans lui, déplacer = détruire + recréer le nœud (perte d'état interne).
  Purement additif, ajouté avec son client.
- **Doublons autorisés** — données ≠ nœuds (`NodeList` force l'unicité parce qu'un nœud
  n'a qu'un parent ; une liste de données non). Conséquence : le registre de `BindItems`
  sera positionnel, pas un `Dictionary<TItem, Node>`.
- **`IReadOnlyReactiveList<T>`** — lecture + événements, sans mutation : le type des
  sources de `BindItems` dans les contrats de scène.

### Durée de vie des bindings et convergence avec l'arbre (décidée en passe 4)

- **Le danger nommé** : un binding est un abonnement, et **la source détient la cible**
  (la liste de delegates de `Changed` pointe vers le handler, qui pointe vers la cible).
  Cible morte sans déliage = zombie (la source écrit dans un nœud mort pour toujours)
  + fuite (tout le sous-arbre mort reste retenu). *Weak events* rejetés : la mort
  dépendrait du timing du GC — on délie de façon **déterministe**.
- **La règle** : un binding vit aussi longtemps que sa cellule **cible** ; la cellule
  meurt avec son nœud propriétaire. `Dispose()` du nœud = `Unbind()` de toutes ses
  cellules. `Detach` ne touche pas aux bindings — un nœud détaché est vivant, continue
  de se synchroniser, se ré-insère à jour. Sens inverse (source morte, cible vivante) :
  la cible gèle sur la dernière valeur — inoffensif, documenté. Un nœud détaché oublié
  sans `Dispose` fuit s'il est bindé — contrat `IDisposable` habituel.
- **Mécanisme côté nœud** : `Node.CreateReactive<T>(initial)`, miroir de
  `CreateChildSlot` — le nœud enregistre ses cellules, son `Dispose` les délie.
  `Reactive` ne connaît pas l'arbre ; c'est le SceneGraph qui branche.
- **Convergence : la mort du `Prune`** — des conteneurs observables exigent des
  événements émis **au moment du départ**, pas au prochain accès (l'auto-réparation
  paresseuse désynchroniserait tout consommateur). Mécanisme retenu — pas de
  back-pointer sur l'enfant : tous les chemins de départ (adoption ailleurs, `Dispose`,
  `Detach` direct) passent déjà par la machinerie de détachement du **propriétaire**,
  et le propriétaire connaît ses conteneurs (`CreateChildSlot`/`CreateChildList` les
  enregistrent, miroir de `CreateReactive`). Au détachement, il leur diffuse « X est
  parti » ; le détenteur se met à jour et émet `Removed` immédiatement. `Prune`
  disparaît, `Count` est exact à tout instant.
- **`NodeSlot<T>` expose `IReadOnlyReactive<TChild?>`, `NodeList<T>` expose
  `IReadOnlyReactiveList<TChild>`** — « observable + sémantique d'arbre »,
  littéralement. Le cas client (scène qui décore ses enfants injectés) = s'abonner
  aux `Added`/`Removed` de sa propre liste.
- **`BindItems(source, fabrique)` : le registre, c'est l'index** — miroir strictement
  positionnel (`target[i]` ↔ `source[i]`), retombée directe de « doublons autorisés »
  + grain exact : pas de dictionnaire item→nœud (le générateur lourd de WPF sert la
  virtualisation/réutilisation — pas la v1). Occupe le slot de binding de la `NodeList`
  (un `Add` manuel sur une liste liée = fail-fast ; `Unbind()` pour reprendre la main).
  `Removed` → **`Dispose`** du nœud fabriqué : la fabrique l'a créé pour le binding,
  le binding le possède (principe 1 — une référence, p. ex. obtenue via `Children[i]`,
  ne confère pas la propriété) ; le détacher créerait un orphelin vivant sans
  responsable. Échappatoire : `Unbind()` d'abord, puis les nœuds sont à toi.
- **Dépendance nouvelle** : SceneGraph → Mathematics + **Reactive** (la flèche va dans
  un seul sens — `Reactive` reste sans dépendance).

### Hors périmètre (volontairement)

La brique UI elle-même (`UINode`, layout, `Switcher`, `ListPanel`) — elle *consommera*
ces primitives dans sa propre roadmap. Le rendu (triangle Vulkan) reste l'autre
chantier, indépendant.

## Blocs

- [x] **Bloc 0 — Nouveau projet `HumbleEngine.Reactive`** ✅ (build vert, 116 tests unitaires verts)
  - Classlib net10.0, `RootNamespace` HumbleEngine, aucune dépendance (modèle Mathematics)
  - Ajout à la solution, référencé par `HumbleEngine.Tests`
  - Mise à jour CLAUDE.md (structure, dépendances, phase courante)
  - Validation : build + tests verts

- [x] **Bloc 1 — Conception** ✅ (passes progressives)
  - [x] Passe 1 : panorama des modèles réactifs ✅ → section « Panorama » ci-dessus
  - [x] Passe 2 : `Reactive<T>` + bindings scalaires ✅ → section « Conception de
    `Reactive<T>` » ci-dessus
  - [x] Passe 3 : `ReactiveList<T>` ✅ → section « Conception de `ReactiveList<T>` » ci-dessus
  - [x] Passe 4 : durée de vie + convergence ✅ → section « Durée de vie des bindings
    et convergence avec l'arbre » ci-dessus

- [x] **Bloc 2 — `Reactive<T>` + bindings scalaires** ✅ (`BindFrom`, `BindTwoWayFrom`,
  `Unbind`, `IReadOnlyReactive<T>` — 146 tests unitaires verts au total)

- [ ] **Bloc 3 — `ReactiveList<T>`** + tests

- [ ] **Bloc 4 — `BindItems` + registre item→nœud, convergence `NodeSlot`/`NodeList`** + tests
  (la partie qui vit côté SceneGraph)
