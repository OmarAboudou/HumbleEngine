# Roadmap — SceneGraph (cœur données + math)

Objectif : concevoir et implémenter le cœur du SceneGraph — types mathématiques,
nœuds, hiérarchie, scène et parcours — inspiré du modèle de Godot,
en évitant ses défauts identifiés. L'intégration au rendu est **hors périmètre** :
elle attendra que le pipeline Vulkan sache dessiner un triangle.

Découpage en blocs — chaque bloc compile, est testé et validé avant de passer au suivant.

## Décisions de conception

- **Math maison** — `Vector3`, `Quaternion`, `Matrix4x4` écrits à la main (cœur pédagogique de la phase), pas de `System.Numerics`.
- **Deux nouveaux projets** — `HumbleEngine.Mathematics` (autonome, modèle GLM : tout le monde en a besoin, elle n'a besoin de personne) + `HumbleEngine.SceneGraph` → Mathematics.
- **Renommage préalable** — `HumbleEngine.Core` devient `HumbleEngine.HAL` : avec Mathematics à côté, « Core » ne décrit plus le contenu (contrats de la couche matérielle). La règle « abstractions uniquement » est conservée telle quelle.
- **Namespaces inchangés** — le nom d'assembly décrit le rangement physique, pas le namespace (`HumbleEngine`, `HumbleEngine.Linux`, … ne bougent pas).
- **Cap produit (décidé 2026-06-12)** — le moteur servira d'abord à faire des **applications UI**, puis son propre **éditeur** (dogfooding), puis la 2D. Les passes de conception gardent ce cap en tête (place de la 2D, layout, binding).

### Principes retenus (retour d'expérience Godot)

1. **L'arbre possède** — une scène déclare des **slots** typés dans son contrat public (modèle React `children`) : injecter dans un slot transfère la propriété, et le propriétaire détruit ce qu'il possède. Une référence ne confère pas la propriété. L'intérieur d'une scène reste privé, seul son contrat est public.
2. **Injection, jamais lookup** — les dépendances entrent par slots/paramètres ; pas d'équivalent de `get_node("../X")` comme mécanisme de dépendance.
3. **Le système de types C# est le seul registre** — interfaces first-class partout, y compris dans les contrats de slots ; aucune fonctionnalité future (sérialisation, inspecteur) ne contournera le système de types .NET avec un registre parallèle.
4. **Un seul arbre, retenu et réactif** *(révisé en passe 4)* — l'UI vit dans l'arbre unique (`UINode : Node`),
   mais le défaut Godot (arborescences géantes écrites à la main, nœuds lourds) est traité autrement :
   construction déclarative one-shot dans le code, nœuds légers, et encapsulation par scènes fermées
   (l'arbre profond d'un composant est invisible). Les mises à jour passent **exclusivement par les
   bindings** — la structure est *éditée* (primitives réactives : `Switcher`, `ListPanel` + fabriques…),
   jamais redécrite. Modèle WPF/Avalonia/SolidJS, pas de widget layer jetable ni de réconciliation
   globale ; une réutilisation d'instances locale et opt-in (par clé, zones de rebuild) pourra
   s'ajouter plus tard sans toucher au cœur — l'état des zones dynamiques vit dans le modèle, pas dans la vue.

### Conventions mathématiques (décidées en passe 2)

- **Repère** : main droite, **Y-up**, la caméra regarde **−Z** (convention Godot / OpenGL / glTF — imports d'assets sans conversion).
- **Vecteurs colonne** : `v' = M·v`, composition `world = parent · local`, `M = T·R·S` lue de droite à gauche.
- **Stockage column-major** : layout mémoire identique à ce qu'attend GLSL/Vulkan (std140) — upload GPU = copie brute, sans transposition.
- **Clip space Vulkan** (Y bas, profondeur [0,1]) : absorbé exclusivement par la matrice de projection — aucune contamination du monde.
- **Radians partout en interne** ; degrés uniquement en confort d'API.
- **Rotations = quaternions** (quand la 3D arrivera) ; angles d'Euler seulement en entrée/sortie de confort.
- **Types du bloc 2 — périmètre minimal produit** : `Vector2`, `Rect`, `Vector3`, `Vector4`, `Matrix4x4`
  (avec projection orthographique) — des `readonly struct` immuables. Tout ce que l'UI et son rendu exigeront ;
  `Quaternion` et la composition TRS sont différés à la phase 2D/3D, appris juste-à-temps.
- **Mathematics est neutre sur l'espace écran** : le « Y bas, origine en haut à gauche » de l'UI sera une convention de la brique UI.

### Conception du Node (décidée en passe 3)

- **Cycle de vie symétrique en 4 hooks** (méthodes virtuelles protégées), tirés **à chaque** attach/detach —
  pas de sémantique « une seule fois » cachée, contrairement au `_ready` de Godot.
  Convention de nommage : *-ing* = annonce descendante, *-ed* = complétion montante :
  - `OnAttaching` — parent d'abord (top-down) : le nœud entre dans l'arbre, ses ancêtres sont notifiés
  - `OnAttached` — enfants d'abord (bottom-up) : tout le sous-arbre est attaché et prêt
  - `OnDetaching` — parent d'abord (top-down) : annonce de sortie, tout est encore vivant et valide
  - `OnDetached` — enfants d'abord (bottom-up) : démontage effectif
  - Les hooks ne concernent que l'entrée/sortie d'un **arbre vivant** — manipuler un sous-arbre détaché ne tire rien.
- **Un nœud peut vivre hors de tout arbre** (les slots l'exigent : on construit, puis on injecte) —
  attaché : le parent dispose ; détaché : le détenteur de la référence est responsable
  (le GC de .NET adoucit le problème des nœuds orphelins de Godot).
- **Destruction : les deux formes** — `Dispose()` immédiat récursif + `QueueDispose()` différé à un point sûr
  (branché sur la boucle plus tard), pour le cas « le handler détruit son propre conteneur ».
- **`Name` optionnel, non unique** — debug et futur éditeur uniquement, jamais un identifiant
  (le lookup par chemin étant rejeté — principe 2 — l'unicité par parent de Godot n'a pas de client).
- **Reparentage : `Reparent()` dédié, sémantique exacte** — tire toujours `OnParentChanged` (5e hook,
  « qui est mon parent ? » étant un fait distinct de « suis-je dans un arbre vivant ? ») ; ne tire les
  hooks d'arbre que si l'appartenance à un arbre vivant change réellement (même arbre → aucun
  exit/enter ; vers/depuis un sous-arbre détaché → les hooks correspondants). **Les hooks ne racontent
  que des faits.** `Parent` est une référence structurelle ; la dépendance comportementale au type du
  parent est un anti-pattern (principe 2) — les besoins légitimes passent par des protocoles médiés
  (layout, futur contexte hérité). Client du reparentage : l'emballage conditionnel en UI retenue.

### Conception de la Scene et de l'arbre (décidée en passe 4)

- **La classe EST le template** — pas d'équivalent réifié de `PackedScene` : la classe C# est le template,
  `new` est l'instanciation, le typage est gratuit (le `Instantiate()` non typé de Godot disparaît).
  Godot réifie `PackedScene` parce que ses scènes sont des fichiers ; les nôtres sont du code.
- **`Scene : Node` à composition fermée** — une scène est sa propre racine : étant un `Node`, elle est
  insérable comme enfant, injectable dans un slot, notifiée par les hooks et détruite par son
  propriétaire, sans plomberie de délégation. Visibilité (C# ne sait pas *réduire* la visibilité en
  héritant) : le `Node` de base porte la gestion des enfants en `protected` ; les **conteneurs**
  (`Panel`, …) l'exposent publiquement ; `Scene` n'expose rien → fermée. « Fermé par défaut, ouvert
  explicitement » — l'encapsulation du principe 1 est vérifiée par le compilateur, pas par convention.
- **Slots = propriétés typées recevant des instances** — A construit, B adopte (transfert de propriété).
  Interfaces bienvenues dans les contrats (principe 3). Le contenu répété/dynamique relèvera du
  système de widgets, pas des scènes.
- **Construction déclarative one-shot** — la description s'exécute une fois à l'instanciation via les
  initialiseurs C# (exigence d'API : `Children` initialisable par collection) et produit l'arbre vivant,
  qui ensuite *mute*. Déclaratif ≠ réactif : aucune réconciliation au niveau scène (réservée au widget layer).
- **`SceneTree` dédié** — objet racine explicite : détient la racine, marque la frontière du vivant
  (les hooks ne tirent que sous lui), héberge la file de `QueueDispose`, et servira de point de
  branchement fenêtre/boucle plus tard. `SceneGraph` ne dépend que de `Mathematics`, jamais de la HAL.
- **Note de faisabilité éditeur** — l'édition visuelle de scènes restera possible via le pattern
  « designer + classes partielles » (WinForms/WPF) : l'éditeur régénère en bloc un fichier `*.g.cs`
  contenant la construction déclarative, l'humain garde son fichier de logique. Le déclaratif one-shot
  est trivialement imprimable en code, et la réflexion .NET expose les slots typés (interfaces comprises)
  à l'inspecteur. Coût réel reporté à la phase éditeur : compiler/recharger le code utilisateur
  (Roslyn + AssemblyLoadContext / hot reload).

### Hors périmètre (volontairement)

Rendu, caméra, culling, système de composants/ECS, sérialisation et chargement de scènes.
Le **système de binding réactif** (`ObservableProperty<T>`, `BindFrom`) est voulu pour le cap UI
mais aura sa roadmap dédiée après ce cœur.
La **brique UI** (futur `HumbleEngine.UI` : `UINode`, layout, rendu, primitives réactives
structurelles — principe 4) sera conçue après ce cœur ; la réconciliation globale est sortie
du programme (remplacée par les bindings).
Le **`Transform` TRS hiérarchique**, `Node2D`/`Node3D` et `Quaternion` sont différés à la phase 2D/3D —
le cap UI n'en a pas besoin ; le pattern d'invalidation (dirty flags) sera appris sur le layout UI.

## Blocs

- [x] **Bloc 0 — Renommage HAL** ✅ (build vert, 9 tests unitaires + 41 tests d'intégration verts)
  - `HumbleEngine.Core` → `HumbleEngine.HAL`
  - Backends regroupés sous le préfixe : `HumbleEngine.HAL.X11`, `HumbleEngine.HAL.Wayland`,
    `HumbleEngine.HAL.Vulkan`, `HumbleEngine.HAL.OpenGL`, `HumbleEngine.HAL.Linux`
  - `HumbleEngine.Sandbox`, `HumbleEngine.Tests`, `HumbleEngine.Tests.Linux` inchangés
  - Mise à jour : dossiers, `.csproj`, `.sln`, `ProjectReference`, CLAUDE.md
  - Validation : build + tous les tests verts

- [x] **Bloc 1 — Conception** ✅
  - [x] Périmètre et responsabilités ; décisions math maison + deux projets
  - [x] Défauts de Godot à éviter → principes 1-3 ci-dessus (d'autres défauts pourront s'ajouter)
  - [x] Conventions math → section « Conventions mathématiques » ci-dessus
  - [x] Conception `Node` → section « Conception du Node » ci-dessus (sans transform — principe 4 + cap UI)
  - [x] Trancher le reparentage → `Reparent()` + `OnParentChanged`, sémantique exacte (ci-dessus)
  - [x] Conception `Scene` + `SceneTree` + slots → section « Conception de la Scene et de l'arbre » ci-dessus

- [x] **Bloc 2 — HumbleEngine.Mathematics** ✅ (65 tests unitaires verts au total)
  - `Vector2`, `Rect`, `Vector3`, `Vector4`, `Matrix4x4` + projection orthographique (clip space Vulkan absorbé)
  - Layout column-major vérifié par un test mémoire (`MemoryMarshal`) ; conventions du moteur
    (main droite, `Forward = −Z`, composition droite-à-gauche) encodées et testées

- [ ] **Bloc 3 — HumbleEngine.SceneGraph : Node + hiérarchie**
  - `Node` nu : parent/enfants, cycle de vie 4 hooks + `OnParentChanged`, `Dispose`/`QueueDispose`,
    `Name` optionnel, `Reparent()` à sémantique exacte
  - Tests unitaires

- [ ] **Bloc 4 — Scene + SceneTree**
  - `Scene` (composition fermée, slots par instances), `SceneTree` (frontière du vivant,
    file `QueueDispose`), parcours de l'arbre
  - Tests unitaires
