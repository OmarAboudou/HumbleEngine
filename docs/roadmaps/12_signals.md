# Roadmap — Les signals (« tout observable », auto-tracking)

Objectif : tendre vers le modèle React/Solid — l'état du moteur est **observable
par nature**, et le câblage des dépendances est **automatique**. Le levier n'est
pas « `Reactive` partout » mais l'**auto-tracking** : lire une valeur réactive
dans un calcul l'y abonne tout seul. On pose la **fondation** pendant que le
réactif n'est utilisé qu'à quelques endroits (le renommage et la migration sont
petits maintenant, douloureux après l'éditeur).

Découpage en blocs — chaque bloc compile, se voit (tests / Sandbox), validé avant le suivant.

## Décisions de conception (passe 1 — validé 2026-06-13)

### Nommer par le rôle, pas par le mécanisme

- **`Reactive<T>` → `Property<T>`** (validé — proposé par Omar) : on nomme *ce que
  c'est* (la propriété bindable/sérialisable/exposée d'un nœud ou modèle), pas
  *comment ça marche* (réactif). Aligné éditeur (Godot `@export`, WPF
  `DependencyProperty`, MVVM `[ObservableProperty]`) et avec la mémoire
  « ObservableProperty voulu ». Le léger télescopage avec le mot « property » de
  C# est accepté (WPF/MVVM vivent avec).
- **`IReadOnlyReactive<T>` → `IReadOnlyProperty<T>`** : c'est **déjà** la
  « ReadOnlyProperty » d'Omar — la **vue lecture seule observable**. Un
  `Property<T>` exposé en `IReadOnlyProperty<T>` = lecture seule de l'extérieur,
  le propriétaire garde l'écriture. **L'exposition éditeur s'applique à ces
  propriétés** : `Property` éditable (lecture/écriture), `IReadOnlyProperty` et
  `Computed` en lecture seule.
- **Le projet `HumbleEngine.Reactive` reste** : c'est le **runtime de
  réactivité** (Property, Computed, Effect y vivent tous) — là, le nom-mécanisme
  est juste pour le *sous-système*, même si le type exposé est nommé par son rôle.
- `ReactiveList<T>` → **`ObservableList<T>`** (+ `IReadOnlyObservableList<T>`),
  `Node.CreateReactive` → `CreateProperty`. `IReactiveCell` (interne) reste la
  plomberie du graphe.

### La famille à trois rôles (axes orthogonaux)

- **`Property<T>`** — la **source** : état stocké, lecture/écriture, exposée
  éditable. (axe *droit d'écriture*)
- **`Computed<T> : IReadOnlyProperty<T>`** — la **dérivée** : une **formule** sur
  d'autres réactifs, recalculée toute seule (auto-tracking), lecture seule, pas
  sérialisée. `Computed = ReadOnlyProperty + câblage des dépendances automatique`.
  (axe *dérivation* — orthogonal au précédent)
- **`Effect`** — le **puits** : une action ré-exécutée quand ses dépendances (lues
  dedans) changent ; le **pont** vers l'impératif (`window.SetTitle`, autosave).

### Pourquoi maintenant, et jusqu'où (le curseur)

Le moteur **redessine chaque frame** : `Computed`/`Effect` ne sont **pas porteurs
pour le rendu** (on lit `.Value` chaque frame). Leur gain est le **layout**
(remplace le câblage `.Changed` manuel du `LinearContainer`, dès maintenant) et
surtout les **formulaires + l'éditeur** (validation, affichage dérivé, autosave,
undo). Donc deux étages, **curseur d'Omar sur la profondeur** :
- **Étage bas (certain)** : renommage + `Property`/`IReadOnlyProperty`. Sert
  l'exposition, propre, mécanique.
- **Étage haut** : l'auto-tracking (`Effect`/`Computed`) + rebrancher l'existant.
  Le cœur du « tout observable » ; pose la fondation et nettoie le layout actuel.

## Blocs

- [ ] **Bloc 1 — Conception** — passe 1 ci-dessus (nommage + famille + curseur).
- [x] **Bloc 2 — Le renommage** ✅ — `Reactive<T>`→`Property<T>`,
  `IReadOnlyReactive<T>`→`IReadOnlyProperty<T>`, `ReactiveList`→`ObservableList`
  (+ `IReadOnlyObservableList`), `CreateReactive`→`CreateProperty` ; fichiers
  renommés (`Property.cs`, `ObservableList.cs`, …) ; `IReactiveCell` (interne) et
  le projet `HumbleEngine.Reactive` gardés. ~17 fichiers clients à jour, **252
  tests verts, zéro changement de comportement**. *Fin de l'étage bas.*
- [x] **Bloc 3 — L'auto-tracking : contexte + `Effect`** ✅ — `Tracking` (contexte
  courant thread-statique) + `IReactiveSource` + `Computation` (run en trackant,
  re-track à chaque passe, `Invalidate` à la dépendance, garde `_running` contre
  l'auto-récursion). `Property.Value` getter appelle `Track(this)` (no-op hors
  effet) ; le setter invalide les observers après `Changed`. `Effect` public
  (IDisposable, run immédiat). 7 tests : run unique, ré-exécution sur dépendance
  lue, ignore les non-lues, **re-track dynamique** (drop des sources périmées),
  dispose, pas de boucle sur auto-écriture. **259 unitaires verts.**
- [ ] **Bloc 4 — `Computed<T>`** — cellule dérivée au-dessus du tracking : un
  `Effect` qui écrit une valeur cachée et est lui-même observable (source pour
  l'aval). Évaluation paresseuse vs avide, diamants, ré-entrance/cycles (on a déjà
  la détection en two-way à étendre).
- [ ] **Bloc 5 — Brancher l'existant** — remplacer le câblage manuel du
  `LinearContainer` (un `Effect` qui re-tracke les enfants/Spacing → `Relayout`)
  et de `Label`/`TextField` (mesure dérivée), comme dogfood + preuve. *Fin de
  l'étage haut.*

*Tâche en cours : bloc 1 (conception) fait ; prochain — bloc 2 (le renommage),
puis curseur sur l'étage haut.*
