# Flutter — Fiche de révision

> Axe 2 — Systèmes UI & frameworks applicatifs
> Dernière mise à jour : Tour 1 (Vulgarisation) ✅

---

## L'idée centrale

**Tout est Widget.** Un bouton, un texte, une marge, une mise en page, une animation — tout est un Widget. Flutter construit l'UI comme un arbre de Widgets imbriqués, et dessine le résultat pixel par pixel via son propre moteur graphique.

---

## Les concepts fondamentaux

| Concept | Définition courte |
|---------|------------------|
| **Widget** | La brique universelle. Décrit un morceau d'interface — pas d'objet séparé pour le style ou la mise en page. |
| **StatelessWidget** | Widget dont les données ne changent pas. Reconstruit si les données extérieures changent. |
| **StatefulWidget** | Widget qui possède un State interne pouvant évoluer. Se reconstruit quand le State change. |
| **State** | L'objet séparé du StatefulWidget qui contient les données dynamiques. |
| **Arbre de Widgets** | L'UI entière, décrite comme un arbre de Widgets imbriqués. |

---

## Le Widget — La brique universelle

Un Widget est une **description** d'un morceau d'interface, pas un élément affiché directement. Flutter prend l'arbre de descriptions et génère le rendu.

```
MaterialApp
└── Scaffold
    ├── AppBar
    │   └── Text("Mon Application")
    └── Column
        ├── Text("Bonjour")
        ├── SizedBox(height: 16)
        └── ElevatedButton
            └── Text("Cliquez ici")
```

Chaque niveau est un Widget — y compris `Column` (mise en page), `SizedBox` (espace vide), `Scaffold` (structure d'écran).

---

## Les types de Widgets

| Famille | Rôle | Exemples |
|--------|------|---------|
| **Affichage** | Montrer quelque chose | `Text`, `Image`, `Icon` |
| **Interaction** | Réagir à l'utilisateur | `ElevatedButton`, `TextField`, `Checkbox` |
| **Mise en page** | Organiser les enfants | `Column`, `Row`, `Stack`, `Padding` |
| **Structure** | Cadre de l'écran | `Scaffold`, `AppBar`, `Drawer` |

---

## Stateless vs Stateful

**StatelessWidget** — Données statiques. Si les données changent, on recrée le Widget depuis l'extérieur.

**StatefulWidget** — Possède un **State** interne qui peut évoluer. Quand le State change, Flutter reconstruit automatiquement l'arbre de Widgets concerné.

```dart
// StatelessWidget : affiche, ne change jamais de lui-même
class MonTexte extends StatelessWidget {
  final String message;
}

// StatefulWidget : possède un compteur qui évolue au clic
class Compteur extends StatefulWidget {
  // Le State contient la valeur, Flutter reconstruit l'UI à chaque changement
}
```

---

## Le rendu pixel par pixel

Flutter ne délègue pas le rendu au système d'exploitation. Il dessine **tout lui-même**, via son propre moteur graphique (Skia / Impeller).

Conséquence : **UI identique sur toutes les plateformes** — Android, iOS, Windows, macOS, Web.

---

## Comparaison des systèmes UI

```
HTML/CSS   → arbre de balises + règles de style séparées
WPF/XAML   → arbre XAML + styles + Data Binding C#
Flutter    → arbre de Widgets (tout est Widget, contenu + apparence unifiés)
```

La grande différence de Flutter : **pas de séparation structure/style** — tout est dans le Widget. Et rendu indépendant du système.

---

## À retenir absolument

1. **Tout est Widget** — y compris la mise en page et les marges.
2. Un Widget est une **description** d'interface, pas un élément affiché directement.
3. **StatelessWidget** = données statiques. **StatefulWidget** = State interne dynamique.
4. Quand le State change, Flutter **reconstruit** l'arbre de Widgets concerné.
5. Flutter dessine **pixel par pixel** lui-même — rendu identique sur toutes les plateformes.

---

## Quiz — Questions clés

- Quelle est la philosophie centrale de Flutter en une phrase ?
- Quelle est la différence entre un StatelessWidget et un StatefulWidget ?
- Dans Flutter, comment gère-t-on la mise en page ?
- Pourquoi l'UI Flutter est-elle identique sur Android, iOS et Windows ?
- Cite une différence fondamentale entre Flutter et HTML/CSS.

---

*Tour 2 (intermédiaire) : cycle de vie des Widgets, BuildContext, InheritedWidget, gestion d'état (Provider, Riverpod…), système de layout en détail (constraints).*
