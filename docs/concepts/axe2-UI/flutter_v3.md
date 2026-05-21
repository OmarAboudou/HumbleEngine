# Flutter — Fiche de révision

> Axe 2 — Systèmes UI & frameworks applicatifs
> Dernière mise à jour : Tour 3 (Technique) ✅

---

## Ce qu'on a vu au Tour 2 (rappel)

| Concept | Définition courte |
|---------|------------------|
| **Widget** | La brique universelle. Décrit un morceau d'interface. |
| **StatelessWidget** | Widget dont les données ne changent pas. |
| **StatefulWidget** | Widget qui possède un State interne pouvant évoluer. |
| **BuildContext** | Position du Widget dans l'arbre — accès aux ancêtres. |
| **Constraints** | Descendent du parent → l'enfant choisit sa taille → le parent positionne. |
| **InheritedWidget** | Brique native pour partager des données sans prop drilling. Base de Provider et Theme. |

---

## 1. Le système de rendu — les trois arbres

Flutter maintient **trois arbres en parallèle** :

```
Widget Tree          Element Tree         RenderObject Tree
────────────         ────────────         ─────────────────
Description          Instance vivante     Rendu effectif
(immuable)           (état, cycle de vie) (layout, paint)

Text("Bonjour")  →   TextElement      →   RenderParagraph
Column(...)      →   ColumnElement    →   RenderFlex
Padding(...)     →   PaddingElement   →   RenderPadding
```

| Arbre | Rôle | Coût |
|-------|------|------|
| **Widget Tree** | Description de l'UI — ce que tu écris | Bon marché, reconstruit souvent |
| **Element Tree** | Instance vivante, porte le BuildContext et le State | Persiste entre les reconstructions |
| **RenderObject Tree** | Layout + paint effectif | Coûteux, mis à jour uniquement si nécessaire |

### Ce qui se passe à chaque setState()

```
setState() appelé
        ↓
Widget Tree reconstruit  ← bon marché, toujours
        ↓
Flutter compare avec l'Element Tree existant
        ↓
RenderObject mis à jour  ← seulement si nécessaire
```

Reconstruire des Widgets ne coûte presque rien. Mettre à jour les RenderObjects coûte.

---

## 2. Keys

Flutter identifie les Widgets par leur **type et leur position**. Si tu réordonnes des Widgets du même type, Flutter peut confondre leurs States.

### La solution

Une **Key** donne une identité stable à un Widget, indépendante de sa position.

```dart
// Sans Key — les States peuvent se mélanger lors d'un réordonnancement
Column(children: [ColorBox(), ColorBox()])

// Avec Key — chaque Widget retrouve son State, quelle que soit sa position
Column(children: [
    ColorBox(key: ValueKey('rouge')),
    ColorBox(key: ValueKey('bleu')),
])
```

### Types de Keys

| Key | Usage |
|-----|-------|
| `ValueKey(valeur)` | Identifie par une valeur — pour les listes dynamiques |
| `ObjectKey(objet)` | Identifie par un objet — quand la valeur peut changer |
| `UniqueKey()` | Force la recréation du State à chaque build |
| `GlobalKey` | Accès à un State depuis n'importe où — à éviter sauf cas exceptionnel |

**Règle pratique** : `ValueKey` sur les éléments de listes réordonnables ou supprimables.

---

## 3. Animations avancées

### Les trois briques fondamentales

**`AnimationController`** — la source du temps (valeur 0.0 → 1.0 sur une durée).

```dart
late AnimationController _controller;

@override
void initState() {
    super.initState();
    _controller = AnimationController(
        duration: const Duration(milliseconds: 400),
        vsync: this,  // synchronise avec le framerate (nécessite TickerProviderStateMixin)
    );
}

@override
void dispose() {
    _controller.dispose();
    super.dispose();
}
```

**`Tween`** — transforme la valeur 0→1 en n'importe quelle plage.

```dart
Animation<double> _opacite = Tween<double>(begin: 0.0, end: 1.0)
    .animate(_controller);

Animation<Offset> _glissement = Tween<Offset>(
    begin: Offset(-1, 0),
    end: Offset.zero,
).animate(CurvedAnimation(parent: _controller, curve: Curves.easeOut));
```

**`AnimatedBuilder`** — reconstruit uniquement le Widget concerné à chaque frame.

```dart
AnimatedBuilder(
    animation: _controller,
    builder: (context, child) {
        return Opacity(
            opacity: _opacite.value,
            child: child,  // child n'est pas reconstruit à chaque frame
        );
    },
    child: const Text('Bonjour'),  // passé tel quel
)
```

### Déclencher une animation

```dart
_controller.forward();   // joue 0 → 1
_controller.reverse();   // joue 1 → 0
_controller.repeat();    // boucle
_controller.reset();     // revient à 0
```

---

## 4. Gestion d'état avancée — Riverpod

### L'idée centrale

Des **providers** déclarés en dehors de l'arbre — sources de données réactives auxquelles n'importe quel Widget peut s'abonner.

```dart
// Déclaration — global, en dehors de tout Widget
final scoreProvider = StateProvider<int>((ref) => 0);

final joueurProvider = FutureProvider<Joueur>((ref) async {
    return await fetchJoueur(); // async géré automatiquement
});
```

### Lecture dans un Widget

```dart
class ScoreWidget extends ConsumerWidget {
    @override
    Widget build(BuildContext context, WidgetRef ref) {
        final score = ref.watch(scoreProvider); // s'abonne, se reconstruit si ça change
        return Text('Score : $score');
    }
}
```

### Modification

```dart
ref.read(scoreProvider.notifier).state += 10;
```

### Comparaison des approches

| Approche | Portée | Cas d'usage |
|----------|--------|-------------|
| `setState` | Local au Widget | État simple, local |
| `InheritedWidget` | Sous-arbre | Bas niveau, rarement direct |
| `Provider` | App entière | Petits/moyens projets |
| `Riverpod` | App entière, indépendant de l'arbre | Projets larges, async, testabilité |

---

## 5. Compilation et performance

### AOT vs JIT

| Mode | Quand | Caractéristiques |
|------|-------|-----------------|
| **JIT** | Développement | Compilation à la volée — hot reload possible |
| **AOT** | Production (release) | Compilé en code machine natif — rapide, pas de hot reload |

### `const` — première optimisation

Un Widget `const` est instancié **une seule fois** et jamais reconstruit.

```dart
// ✅ const — instancié une fois, jamais reconstruit
const Text('Titre fixe')
const Icon(Icons.home)
const EdgeInsets.all(16)

// ⚠️ pas const — reconstruit si le parent se reconstruit
Text(variable)
```

**Règle** : toujours marquer `const` ce qui ne dépend d'aucun état.

### `RepaintBoundary` — isoler les zones animées

Crée une couche de rendu isolée — ce qui est à l'intérieur se repeint sans affecter le reste.

```dart
RepaintBoundary(
    child: AnimatedLogo(),  // repeint 60 fois/seconde sans impacter la page
)
```

### Le Profiler Flutter

Ne jamais optimiser à l'aveugle. Flutter DevTools montre :
- Les rebuilds inutiles (Widgets qui se reconstruisent trop souvent)
- Les jank frames (frames > 16ms)
- La consommation mémoire

---

## À retenir absolument

1. **Trois arbres** : Widget (description) → Element (instance vivante) → RenderObject (layout/paint). Reconstruire des Widgets est bon marché.
2. **Key** = identité stable d'un Widget, indépendante de sa position. `ValueKey` pour les listes dynamiques.
3. **AnimationController** = source du temps. **Tween** = transformation de la plage. **AnimatedBuilder** = reconstruction ciblée.
4. **Riverpod** = providers indépendants de l'arbre, réactifs, testables, async natif.
5. **AOT** en production = code machine natif. **JIT** en dev = hot reload.
6. **`const`** = instancié une fois, jamais reconstruit — à appliquer partout où c'est possible.
7. **`RepaintBoundary`** = isolation d'une zone de rendu coûteuse.

---

## Quiz — Questions clés

- Quel est le rôle de l'Element Tree par rapport au Widget Tree ?
- Pourquoi a-t-on besoin de Keys dans une liste de Widgets StatefulWidgets réordonnables ?
- À quoi sert `AnimatedBuilder`, et quel est son avantage par rapport à reconstruire tout le Widget ?
- Quelle différence entre `ref.watch()` et `ref.read()` dans Riverpod ?
- Pourquoi un Widget `const` n'est-il jamais reconstruit ?
- Dans quel cas utilise-t-on `RepaintBoundary` ?

---

*Synthèse finale : comparaison de tous les systèmes UI, choix argumenté pour HumbleEngine.*
