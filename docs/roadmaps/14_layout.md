# Roadmap — Le layout (contraintes ↓ / tailles ↑)

> **TERMINÉE (2026-06-14)** — deuxième phase de l'ère éditeur. La 13 a posé un
> éditeur debout avec des tailles **en dur** ; ici on lui a donné un vrai moteur de
> layout : **redimensionner la fenêtre replace les panneaux** et les conteneurs se
> mesurent à partir de leur contenu. Blocs 1-5 faits (371 tests verts). Différés :
> `MinSize`/`MaxSize`, `Move`/`Moved`, contexte hérité (`InheritedData`, roadmap 15).

## Cadrage (tranché avec Omar le 2026-06-14)

### Le modèle : Flutter, mais piloté par signals

On adopte le modèle **« constraints down, sizes up »** de Flutter, et **pas** le
« le parent écrit une `Size` exacte » (serré-only, cul-de-sac dès qu'on veut le
retour à la ligne du texte ou des min/max). Deux sens qui ne se croisent jamais :
le parent **descend une contrainte** (min/max), l'enfant **calcule sa taille**
dedans et la **remonte**, le parent le positionne.

### Le protocole : `ComputeLayout` + une computation par nœud

Pas d'effet de layout unique au sommet : un effet auto-tracké doit *lire pour
s'abonner*, donc skipper un sous-arbre lui ferait **perdre l'abonnement**. À la
place, **une computation de layout par nœud** — la forme déjà établie au bloc 1 de
l'éditeur (`Relayout` était un `CreateEffect` par conteneur). Deux `Property` par
nœud et une méthode à override :

```csharp
public Property<Constraints> Incoming { get; }   // canal ↓ : posé par le parent (et observable)
public Property<Vector2>     Size     { get; }    // résultat ↑ : lu par rendu/hit-test/GlobalRect

// Base — câble la computation une fois : lit Incoming, écrit Size.
CreateEffect(() => Size.Value = ComputeLayout(Incoming.Value));

// Chaque nœud n'override que ça. Il reçoit la contrainte EN PARAMÈTRE.
protected abstract Vector2 ComputeLayout(Constraints c);
```

- Un **conteneur** dans son `ComputeLayout` **pose `child.Incoming.Value`** (au lieu
  d'appeler quoi que ce soit en direct) et **lit `child.Size`** pour empiler/se
  mesurer, puis pose `child.Position`. La racine est semée par la surface :
  `CreateEffect(() => Root.Incoming.Value = Constraints.Tight(SurfaceSize.Value))`.
- **Acyclique** : on **écrit** `child.Incoming` (↓) et on **lit** `child.Size` (↑) —
  deux propriétés différentes ; rien ne fait dépendre `child.Size` de `parent.Size`.
  Anti-rétroaction : `ComputeLayout` lit le **paramètre** (jamais `this.Incoming`
  dans la méthode), et le nœud écrit sa `Size` sans jamais la relire.
- **Le skip-unchanged est gratuit** : nos cellules ne notifient que sur changement
  réel → la propagation **s'arrête à la première valeur qui ne bouge pas**
  (`Incoming` ↓, `Size` ↑). Un nœud dont l'allocation est identique n'est pas
  relancé ; un nœud dont la `Size` ne change pas ne réveille pas son parent. Ça
  **subsume les *relayout boundaries*** de Flutter — on ne part jamais de la racine,
  le « boundary » est là où la taille se stabilise.
- **Pas de `dry`** dans le cœur : mesurer un enfant = lui poser une contrainte
  desserrée (qui est sa contrainte finale s'il n'est pas flex) et lire sa `Size`.
  Seul l'**étirement sur l'axe croisé** (mesurer le max, puis re-poser une
  contrainte serrée) fait un re-set transitoire — assumé (philosophie « eager,
  transients OK »).
- Coût honnête : une `Property Incoming` + une computation **par nœud**, et la
  cascade *eager* (poser `child.Incoming` → l'enfant se recalcule tout de suite → on
  relit `child.Size`) à tester avec soin.

### Pas de `DesiredSize` ni de `PreferredSize`

`DesiredSize` n'était que « la taille sous contrainte desserrée » → on la **demande
à la volée** (poser une contrainte desserrée, lire la `Size`), plus besoin de la
persister. `PreferredSize` était un faux concept (un `MinSize` déguisé). Les
**seules** propriétés universelles sont `Incoming` (la contrainte courante,
observable) et `Size` (le résultat). `Size` cesse d'être une entrée qu'on pose :
elle devient le **résultat** (`OnDraw`/`GlobalRect`/hit-test la lisent, inchangé).
La taille *voulue* d'une feuille explicite (`Panel`…) est son affaire — un champ à
elle, consommé dans son `ComputeLayout` (`c.Constrain(voulu)`) — pas une propriété
universelle sur `UINode`.

### Le flex sans polluer les nœuds : enveloppes-descripteurs → parent-data

`weight` n'a de sens que sous un `Row`/`Column` — donc **pas** de propriété sur
tout `UINode`. On suit Flutter : `Expanded`/`Flexible` sont des **descripteurs**
(types valeur), **pas des `Node`** — ils se **dissolvent** à l'ajout en
**parent-data posée sur l'enfant**. L'arbre ne contient que le vrai enfant →
**aucun saut** au hit-test/rendu (chez Flutter c'est gratuit grâce à deux arbres ;
nous n'en avons qu'un, d'où le refus d'une enveloppe-`Node`).

- **Fabrique virtuelle, injection à l'adoption.** `Node` gagne
  `protected virtual ParentData? CreateParentData() => null;`. Un conteneur
  l'override (`LinearContainer` → `new FlexParentData()`). L'injection/retrait se
  fait **dans l'abstraction**, au **goulot d'adoption** (là où `child.Parent` est
  posé/effacé) — donc NodeList **et** NodeSlot le partagent, sans abonnement
  `Added`/`Removed` par conteneur. C'est le `setupParentData` de Flutter.
  ```
  child.Parent = p    → child.ParentData = p?.CreateParentData();
  child.Parent = null → child.ParentData = null;
  ```
  Le détachement passant par un **goulot unique** (retrait, `Clear`, adoption
  ailleurs, nœud disposé), le nettoyage est total et sans fuite.
- **`FlexParentData` = un sac de `Property`**, pas une `Property` de struct :
  ```csharp
  sealed class FlexParentData : ParentData
  {
      public Property<float> Factor { get; } = new(0f);   // 0 = non-flex (content-sized)
      public Property<bool>  Tight  { get; } = new(true);  // flex : remplit sa part (Expanded) vs peut être plus petit (Flexible)
  }
  ```
  Chaque champ est une cellule réactive indépendante → le layout auto-track, éditer
  un champ relance la passe, **et le poids est animable gratuitement** (c'est une
  `Property`). L'inspecteur découvre une ligne par champ.
- **`Expanded`/`Flexible`** exigent leur enfant (non-null ; une enveloppe vide n'a
  pas de sens). `row.Add(new Expanded(child, factor: 2))` ajoute l'enfant
  (→ fabrique la parent-data par défaut) puis remplit `Factor`/`Tight`.

### La parent-data dans l'inspecteur (contextuel, gratuit)

`NodeInspector`, après les propriétés propres du nœud, **énumère aussi** les
`IObservableValue` du slot `node.ParentData`. Le slot étant `null` hors d'un flex,
c'est **contextuel par construction** : rien d'affiché sauf sous un `Row`/`Column`,
où apparaissent `Factor`/`Tight`, éditables et live. Pas d'interface, pas de
routage vers le parent.

### La surface descend jusqu'à la racine

La fenêtre a déjà `Width`/`Height` + `OnResize`
([IWindow](../../HumbleEngine.HAL/Surfaces/IWindow.cs)), mais rien ne le fait
entrer dans l'arbre. On ajoute un pont `OnResize → Property<Vector2> SurfaceSize`
sur `SceneTree`, qui sème `Root.Incoming` (contrainte **serrée** = taille fenêtre).
Tout le reste en découle par descente. Le **rendu reste découplé** : la boucle
dessine chaque frame l'état courant (`Size`/`Position`/`GlobalRect`) ; le layout
réactif garde juste ces valeurs à jour, il ne « déclenche » pas de repaint.

## Les blocs

Chacun compile, **se voit** dans le Sandbox, et est validé avant le suivant.

### Bloc 1 ✅ — `Constraints`, le protocole par nœud, la surface

Le value type `Constraints` (min/max W/H ; fabriques `Tight`/`Loose`/`Unbounded` ;
`Constrain(size)` ; `Loosen()`). Le câblage par nœud sur `UINode` : `Incoming`
(`Property<Constraints>`), la computation `CreateEffect(() => Size.Value =
ComputeLayout(Incoming.Value))`, l'abstrait `ComputeLayout`. Migration : `Size`
passe d'entrée posée à **résultat** ; une feuille explicite (`Panel`) porte sa
taille voulue et fait `ComputeLayout = c.Constrain(voulu)`. Le pont
`OnResize → SurfaceSize` qui sème `Root.Incoming`. *Démo : un `Panel` contraint à
la fenêtre, qui suit le resize.*

### Bloc 2 ✅ — Les conteneurs : `LinearContainer.ComputeLayout`

`ComputeLayout` d'un `Column`/`Row` : poser `child.Incoming` (desserré), lire les
`child.Size`, empiler (somme + `Spacing`), poser les `Position`, retourner sa
taille. Pas encore de flex — content-sizing pur. L'ancien `Relayout` est remplacé.
*Démo : les conteneurs imbriqués de la scène échantillon s'empilent correctement
(fin du chevauchement `Body` à taille nulle).*

### Bloc 3 ✅ — Le flex qui descend

`ComputeLayout` d'un flex container : mesurer les enfants non-flex (contrainte
desserrée), calculer l'espace libre, descendre des contraintes **serrées** aux
enfants flex (part au prorata du `Factor`), remplir l'axe croisé. Parent-data :
`virtual CreateParentData()` injectée au goulot d'adoption ; `FlexParentData` ;
descripteurs `Expanded`/`Flexible`. *Démo : la coque de l'éditeur = un `Row` qui
remplit la fenêtre, hiérarchie + inspecteur fixes, **viewport extensible** — il
s'étire au resize.*

### Bloc 4 ✅ — Parent-data dans l'inspecteur

`NodeInspector` énumère en plus les `IObservableValue` de `node.ParentData` (groupe
« Layout »). Contextuel : `null` → rien. *Démo : sélectionner le viewport (enfant
flex) → l'inspecteur montre `Stretch`/`Factor`, éditables, relayout live.*

### Bloc 5 ✅ — Rebranchement de l'éditeur + nettoyage + tests

`HierarchyDemoScene` en conteneurs (constantes en dur supprimées, la coque reflue
au resize). Nettoyage : `InspectorView.SyncLayout` cesse d'écrire `_rows.Size` ; on
allège le calcul de taille fait à la main dans `HierarchyRow`/`InspectorRow` là où
un sous-conteneur mesuré le permet (suppression complète = quand viendront les
marges — hors roadmap). Passe de tests complète.

## Décisions tranchées (récap)

- **Constraints ↓ / Size ↑** (Flutter), pas le « parent écrit Size » serré-only.
- **Une computation de layout par nœud** : `Incoming` (`Property<Constraints>`, ↓) +
  `CreateEffect(() => Size.Value = ComputeLayout(Incoming.Value))` ; `ComputeLayout(c)`
  override, lit le **param** (pas `this.Incoming`).
- **Skip-unchanged et *relayout boundaries* gratuits** via l'égalité de valeur des
  cellules (la propagation s'arrête où la taille se stabilise).
- **Anti-rétroaction structurel** : on écrit `child.Incoming` (↓), on lit `child.Size`
  (↑) ; le nœud n'a jamais à lire sa propre `Size`.
- **Pas de `dry`** (mesure = poser une contrainte desserrée et lire `Size`) ; seul
  l'étirement croisé fait un re-set transitoire, assumé.
- **Pas de `DesiredSize` ni de `PreferredSize`** — propriétés universelles =
  `Incoming` + `Size`. La taille voulue d'une feuille est son affaire (champ propre).
- **Flex = enveloppes-descripteurs** `Expanded`/`Flexible`, **pas des `Node`**,
  dissoutes en **parent-data** via `virtual CreateParentData()` au goulot d'adoption.
- **`FlexParentData` = sac de `Property`** → réactivité + inspecteur par champ.
- **Inspecteur lit la parent-data en plus** des propriétés propres → contextuel.
- **Axe croisé : remplir** par défaut.

## Différés (notés, pas oubliés)

- **`MinSize`/`MaxSize` — les bornes *propres* du nœud** (intrinsèques, distinctes
  des `Constraints` imposées par le parent). `ComputeLayout` d'un tel nœud fera
  `clamp(contenu, MinSize, MaxSize)`. Client : un `Button` « au moins N ».
- **`Move`/`Moved` — verbe réservé dès maintenant.** Réordonner **ne doit pas** être
  retrait+ajout (ça resetterait la parent-data **et** re-fabriquerait les nœuds sous
  `BindItemsFrom`, perdant identité/état). `Move(from, to)` réordonne la liste
  interne **sans ré-adoption** (`Parent` inchangé → `CreateParentData` non rappelé →
  parent-data préservée), avec son **propre** événement `Moved` (la doctrine « deux
  événements suffisent » passe à trois). Implémentation différée à son client : le
  glisser-déposer dans la hiérarchie. Le design actuel est déjà compatible.
- **Contexte hérité (`InheritedData`) → roadmap 15.** Anti-prop-drilling : un ancêtre
  *fournit*, un descendant *lit/s'abonne* (auto-track), résolution par type en
  remontant. **Différent** de la parent-data (ancêtre quelconque vs parent immédiat ;
  pull vs push ; lecteur = descendant vs parent). Client : `EditorState`, enfilé à la
  main dans tous les constructeurs aujourd'hui.
- **Cache du `GlobalRect`** à dirty-flag (l'optimisation Godot) — arbres profonds.
- **Contraintes desserrées pleinement exploitées** : retour à la ligne du texte
  (`Label` utilisant `maxWidth` → hauteur). La plomberie `Constraints` (serré **et**
  desserré) est là dès le bloc 1 ; le flex s'en sert déjà en interne.

Voir [[project-next-scenegraph]] (état éditeur) et [[project-ui-update-model]]
(retenu + signals + primitives, pas de VDOM).
