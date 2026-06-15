# Roadmap — Le contexte hérité (`InheritedData`, anti-prop-drilling)

> **TERMINÉE (2026-06-15)** — petite phase de l'ère éditeur, dans la foulée du
> layout. Donner à un descendant de **lire une donnée fournie par un ancêtre**
> sans la passer de main en main dans tous les constructeurs. Blocs 1 (mécanisme
> `Provide`/`Inherit`/`TryInherit`) et 2 (éditeur rebranché : `Editor : Scene`
> fournit l'`EditorState`, `HierarchyRow`/`InspectorView` l'héritent) faits.
>
> **Dans la foulée** — refonte du flex : la **parent-data** (roadmap 14) est
> abandonnée au profit de **propriétés inline sur `UINode`** (`FlexFactor`/
> `FlexTight`, modèle CSS). Donner les valeurs depuis le code redevient direct
> (`node.FlexFactor.Value = …`) ; `Expanded`/`Flexible` restent en sucre. Le
> mécanisme `ParentData`/`CreateParentData`/`FlexParentData` est **supprimé**
> (plus de client). Voir CLAUDE.md (phase courante) pour l'état gravé.

## Le problème : le prop-drilling

L'`EditorState` (la sélection) est **enfilé à la main** dans toute la chaîne :
`HierarchyView(root, editor)` → `HierarchyRow(node, editor)` → récursivement à
chaque ligne ; `InspectorView(editor)`. Chaque nœud intermédiaire le transporte
sans s'en servir. C'est exactement le *prop-drilling* que l'`InheritedWidget` de
Flutter, le `Context` de React, le `createContext` de SolidJS suppriment.

## Le modèle : fournir ↓, hériter ↑ (pull)

- Un **ancêtre fournit** une valeur, portée par son sous-arbre.
- Un **descendant l'hérite** : il remonte la chaîne des parents et prend le **plus
  proche** fournisseur du **type** demandé.

C'est un mécanisme **distinct de la parent-data** (gravé roadmap 14) — ne pas les
confondre :

| | **ParentData** (flex) | **InheritedData** (ce doc) |
|---|---|---|
| Source | le parent **immédiat** | **n'importe quel** ancêtre (le plus proche) |
| Sens | le parent **écrit** (push) | le descendant **lit** (pull) |
| Lecteur | le parent (pour arranger) | le descendant |
| Clé | une par parent | par **type** |
| But | bookkeeping de layout | données transverses (sélection, thème, DI) |

## Cadrage proposé (à valider)

### L'API

```csharp
// sur Node
public void Provide<T>(T value) where T : class;   // l'ancêtre publie
public T Inherit<T>() where T : class;             // le descendant résout (le plus proche ancêtre)
```

- **Clé = le type `T`.** Un `Provide<EditorState>(state)` ; un `Inherit<EditorState>()`
  plus bas le retrouve. Le plus proche fournisseur l'emporte (shadowing naturel).
  *(Alternative façon React/Solid : un jeton `Context<T>` permettant plusieurs
  fournisseurs du même type — différé, pas de besoin.)*
- **Stockage** : un `Dictionary<Type, object>` paresseux sur le `Node` fournisseur
  (null tant qu'on ne fournit rien — comme le slot parent-data). Pas de nœud
  `Provider` dédié : *n'importe quel* nœud peut fournir.
- **Résolution** : remontée `Parent` jusqu'au premier fournisseur de `T`. O(profondeur),
  comme `GlobalRect`. Introuvable → exception claire (le contrat : on hérite ce qui
  existe) ; un `TryInherit<T>(out)` pour le cas optionnel.

### La réactivité — la décision clé

Les valeurs fournies sont **stables** (posées à la construction de l'ancêtre).
La réactivité passe par les **cellules de la valeur**, pas par la résolution :
`Inherit<EditorState>().Selection.Value` lu dans un effet s'abonne à `Selection`
comme d'habitude. Donc **v1 = une simple remontée d'arbre, non auto-trackée** ; le
descendant résout une fois (à l'attache) et utilise les `Property` de la valeur.

*Différé : re-fourniture réactive* (un ancêtre republie une autre valeur → les
descendants se re-résolvent). Le fournisseur deviendrait une `Property` lue en
tracking. Aucun besoin pour `EditorState` (stable) — on l'ajoutera si un client
l'exige.

### Le timing — la conséquence sur les consommateurs

`Inherit<T>()` exige d'**être dans l'arbre** (avoir des ancêtres). Donc un
consommateur résout **à l'attache** (`OnAttached`), pas au constructeur, et y câble
ses effets. C'est le vrai changement pour les nœuds éditeur : `HierarchyRow` ne
prendra plus `editor` en paramètre — il fera `Inherit<EditorState>()` dans
`OnAttached` et y branchera la surbrillance + le clic.

## Les blocs

### Bloc 1 — Le mécanisme `Provide`/`Inherit`

`Node.Provide<T>` (dict paresseux), `Node.Inherit<T>`/`TryInherit<T>` (remontée
`Parent`). Tests : fournir/hériter à travers des nœuds intermédiaires, le plus
proche l'emporte (shadowing), introuvable → exception / `TryInherit` false,
re-parentage change la résolution.

### Bloc 2 — Brancher l'éditeur dessus

`HierarchyView` (ou la coque) **fournit** l'`EditorState` ; `HierarchyRow`,
`InspectorView` l'**héritent** (à l'attache) au lieu de le recevoir en constructeur.
Le `BindItemsFrom` récursif de la hiérarchie se simplifie (plus de `editor` à
trimballer dans la fabrique). Tests adaptés ; la démo `--demo hierarchy` se comporte
à l'identique (sélection, surbrillance, inspecteur), mais sans enfilage manuel.

## Décisions à trancher (avant le bloc 1)

1. **Clé par type** (proposé) vs jeton `Context<T>`.
2. **Résolution non réactive** en v1 (proposé) vs auto-track de la re-fourniture.
3. **Introuvable → exception** + `TryInherit` (proposé) vs nullable par défaut.
4. **Noms** : `Provide`/`Inherit` (proposé) — ou `Resolve`/`DependOn`/`UseContext` ?
5. **Qui fournit `EditorState`** : la coque `Row` racine, ou `HierarchyView` ?
   (Il faut que tous les consommateurs soient *sous* le fournisseur.)

## Différés (notés)

- **Re-fourniture réactive** (auto-track de la valeur fournie).
- **Jetons `Context<T>`** (plusieurs fournisseurs du même type).
- **Inspecteur de contexte** (voir quelles valeurs un nœud hérite) — éventuel.

Voir [[project-next-scenegraph]] (état éditeur). La parent-data du layout est dans
`docs/roadmaps/14_layout.md` (mécanisme **distinct**, ne pas fusionner).
