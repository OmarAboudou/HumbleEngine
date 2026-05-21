# Flutter — Fiche de révision

> Axe 2 — Systèmes UI & frameworks applicatifs
> Dernière mise à jour : Tour 2 (Intermédiaire) ✅

---

## Ce qu'on a vu au Tour 1 (rappel)

| Concept | Définition courte |
|---------|------------------|
| **Widget** | La brique universelle. Décrit un morceau d'interface. |
| **StatelessWidget** | Widget dont les données ne changent pas. |
| **StatefulWidget** | Widget qui possède un State interne pouvant évoluer. |
| **State** | L'objet séparé qui contient les données dynamiques. |
| **Arbre de Widgets** | L'UI entière décrite comme un arbre de Widgets imbriqués. |

---

## 1. Cycle de vie des Widgets

### StatelessWidget — simple

```
build() appelé
    ↓
Widget affiché
    ↓
Si le parent se reconstruit → build() rappelé
```

Un StatelessWidget n'a pas de vie propre. Il est reconstruit chaque fois que son parent le demande.

### StatefulWidget — plus riche

```
createState()     ← crée l'objet State associé
    ↓
initState()       ← une fois, au démarrage (≈ _ready() Godot)
    ↓
build()           ← construit l'UI à partir du State
    ↓
[Si setState() est appelé]
build()           ← reconstruit l'UI
    ↓
dispose()         ← quand le Widget est retiré de l'arbre
```

```dart
class Compteur extends StatefulWidget {
  @override
  State<Compteur> createState() => _CompteurState();
}

class _CompteurState extends State<Compteur> {
  int _valeur = 0;

  @override
  void initState() {
    super.initState();
    // initialisation une seule fois
  }

  @override
  Widget build(BuildContext context) {
    return Text('$_valeur');
  }

  @override
  void dispose() {
    // nettoyage (timers, streams…)
    super.dispose();
  }
}
```

---

## 2. BuildContext

Le **BuildContext** est passé à chaque méthode `build()`. C'est la **position du Widget dans l'arbre** — une référence à son emplacement, pas au Widget lui-même.

```dart
@override
Widget build(BuildContext context) {
    final theme = Theme.of(context);         // récupère le thème de l'app
    final navigator = Navigator.of(context); // récupère le Navigator parent
    ...
}
```

Il sert principalement à **accéder à des données ou services qui viennent d'un ancêtre** dans l'arbre. Sans context, un Widget est aveugle à ce qui l'entoure.

---

## 3. Le système de layout — Constraints

Flutter suit une règle stricte :

> **Les constraints descendent. Les tailles remontent. Le parent positionne.**

```
Parent → envoie des constraints à l'enfant (taille min/max autorisée)
              ↓
Enfant → choisit sa taille dans ces contraintes
              ↓
Parent → positionne l'enfant
```

```dart
// SizedBox impose une contrainte de taille fixe
SizedBox(
    width: 200,
    height: 100,
    child: Text('Bonjour'),
)

// Expanded prend tout l'espace disponible dans un Row/Column
Row(
    children: [
        Expanded(child: Text('gauche')),  // prend la moitié
        Expanded(child: Text('droite')),  // prend la moitié
    ],
)
```

---

## 4. Gestion d'état

`setState()` fonctionne pour un seul Widget. Si plusieurs Widgets partagent le même état, il faut le remonter dans l'arbre ou utiliser une solution dédiée.

### Le problème — prop drilling

```
AppWidget
└── EcranPrincipal
    └── BarreNavigation
        └── BoutonPanier   ← a besoin du nombre d'articles
```

Faire traverser l'information à travers tous les niveaux intermédiaires les rend lourds (ils connaissent une donnée dont ils n'ont pas besoin) et fortement couplés à leurs ancêtres.

### Les solutions courantes

| Solution | Complexité | Cas d'usage |
|----------|-----------|-------------|
| `setState` | Minimale | État local à un seul Widget |
| `InheritedWidget` | Moyenne | Données partagées (thème, locale…) |
| `Provider` | Faible | Petits/moyens projets |
| `Riverpod` | Moyenne | Projets plus larges, plus robuste |

---

## 5. InheritedWidget

L'**InheritedWidget** est le mécanisme natif de Flutter pour partager des données dans l'arbre sans les passer manuellement à chaque niveau.

```dart
// Déclaration
class MonTheme extends InheritedWidget {
    final Color couleurPrimaire;

    const MonTheme({
        required this.couleurPrimaire,
        required super.child,
    });

    static MonTheme of(BuildContext context) {
        return context.dependOnInheritedWidgetOfExactType<MonTheme>()!;
    }

    @override
    bool updateShouldNotify(MonTheme oldWidget) =>
        couleurPrimaire != oldWidget.couleurPrimaire;
}
```

```dart
// Utilisation depuis n'importe quel enfant
Widget build(BuildContext context) {
    final theme = MonTheme.of(context); // remonte l'arbre automatiquement
    return Container(color: theme.couleurPrimaire);
}
```

C'est la brique de base sur laquelle `Provider`, `Theme` et `Navigator` sont construits dans Flutter.

---

## À retenir absolument

1. `initState()` → une fois. `build()` → à chaque reconstruction. `dispose()` → au retrait.
2. **BuildContext** = position dans l'arbre, permet d'accéder aux ancêtres.
3. **Constraints** : descendent du parent → l'enfant choisit sa taille → le parent positionne.
4. **Prop drilling** = faire traverser une donnée par des Widgets qui n'en ont pas besoin — lourd et couplé.
5. **InheritedWidget** = brique native pour partager des données sans prop drilling. Base de Provider et Theme.

---

## Quiz — Questions clés

- Quelle différence entre `initState()` et `build()` dans un StatefulWidget ?
- À quoi sert le `BuildContext` passé à chaque `build()` ?
- Explique en une phrase la règle des constraints dans Flutter.
- Qu'est-ce que le prop drilling, et pourquoi est-ce un problème ?
- Sur quoi `Provider` et `Theme` sont-ils construits dans Flutter ?

---

*Tour 3 (technique) : système de rendu (RenderObject), keys, animations avancées, gestion d'état avancée (Riverpod), compilation et performance.*
