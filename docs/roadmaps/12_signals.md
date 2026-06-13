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
- **`IReadOnlyReactive<T>` → `IReadOnlyProperty<T>`** : la **vue lecture seule
  observable**. **L'exposition éditeur s'applique à ces propriétés** : `Property`
  éditable, `IReadOnlyProperty`/`Computed` en lecture seule.
- **`Property<T>.AsReadOnly()`** (ajouté — proposé par Omar) : rend un wrapper
  `ReadOnlyProperty<T>` (interne) **non-castable** vers la cellule — lecture seule
  *garantie par le type*, pas par convention (le raisonnement du `ReadOnlyCollection`
  du BCL). **Caché** (le même wrapper réutilisé, mieux que `List.AsReadOnly`),
  forwarde `Value`/`Changed` (donc l'auto-tracking traverse vers la source). Le
  wrapper **n'est pas** `IReactiveSource` (il délègue) — d'où le fait que
  `IReadOnlyProperty` n'hérite *pas* de `IReactiveSource` (en plus de la
  visibilité : `IReactiveSource` est interne).
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
- [x] **Bloc 4 — `Computed<T>`** ✅ — dérivée au-dessus du tracking : une
  `Computation` (consomme les lectures de sa formule) qui est aussi
  `IReactiveSource`/`IReadOnlyProperty` (source pour l'aval). **Avide + synchrone**
  comme le reste du graphe ; ne notifie que si la valeur change vraiment
  (egalité). Re-entrance protégée par la garde `_running` de la `Computation`.
  8 tests : valeur initiale, recompute sur dépendance, `Changed`, **chaîne
  `Property→Computed→Effect`**, **skip si dérivée inchangée**, dérivation chaînée,
  dispose. **266 unitaires verts.**
- [x] **Bloc 5 — Intégration nœud + preuve** ✅ — `Node.CreateEffect(Action)` et
  `Node.CreateComputed<T>(Func<T>)` (durée de vie liée au nœud : disposés à la
  mort du nœud, comme `CreateProperty`). *Sandbox : un `Label` « statut » dont le
  texte est un `Computed` du nombre de caractères d'un champ, recalculé tout seul
  quand on tape, miroité par `BindFrom` — le stack signals de bout en bout, zéro
  `.Changed`.* 2 tests de durée de vie ; **272 unitaires verts.**
  - **Suivi noté : les collections réactives.** Rebrancher le `LinearContainer`
    complet (un `Effect` qui re-tracke les enfants + Spacing + tailles, supprimant
    le bookkeeping `.Changed` add/remove) exige que les collections (`NodeList`,
    `ObservableList`) **participent au tracking** (track à la lecture, invalidation
    à la mutation) — or `IReactiveSource` est interne au projet Reactive. C'est un
    morceau à part (exposer un primitif de tracking aux collections SceneGraph),
    laissé en suivi.

*Roadmap 12 terminée.* Étage bas (renommage + `Property`/`IReadOnlyProperty` +
`AsReadOnly`) et étage haut (`Effect`/`Computed` auto-tracking + intégration nœud)
faits. Suivi : collections réactives (pour le dogfood `LinearContainer`).
