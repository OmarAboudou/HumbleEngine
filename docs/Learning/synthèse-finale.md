# HumbleEngine — Synthèse finale

> Axe 1 + Axe 2 — Comparaisons et choix d'architecture
> Dernière mise à jour : Synthèse (après Tour 3) ✅

---

## Contexte du projet

**Langage cible** : C#
**Scope initial** : moteur d'applications UI interactives (logiciels, interfaces graphiques)
**Ambition long terme** : extensible vers un moteur de jeu complet

---

## Axe 1 — Comparaison des architectures moteur

| Critère | Godot — Node Tree | Unity — GameObject+Components | Unreal — Actor/Component | Bevy — ECS pur |
|--------|:-----------------:|:------------------------------:|:------------------------:|:--------------:|
| **Paradigme** | Arbre de Nodes spécialisés | Composition par empilement | Classes spécialisées + Components | Données / Logique strictement séparées |
| **Couplage données/logique** | Fusionnés dans le Node | Fusionnés dans MonoBehaviour | Fusionnés dans l'Actor | Complètement séparés |
| **Hiérarchie** | Arbre parent/enfant | Arbre parent/enfant | Flat + relations optionnelles | Flat (Archetypes) |
| **Communication** | Signaux découplés | GetComponent / UnityEvents | Delegates / RPCs | Events / Resources |
| **Réutilisabilité** | Scènes instanciables | Prefabs | Blueprints | Systèmes réutilisables |
| **Langage natif** | GDScript / C# | **C#** | C++ / Blueprint | Rust |
| **Performance brute** | Bonne | Bonne → Excellente (DOTS) | Excellente | Excellente (parallélisme natif) |
| **Courbe d'apprentissage** | Faible | Moyenne | Élevée | Élevée |
| **Adapté à l'UI** | Moyen (nodes `Control`) | Moyen | Faible | Faible |
| **Adapté à C#** | ✅ Oui | ✅ **Natif** | ❌ C++ first | ❌ Rust |

### Ce qu'on retient de chaque moteur

| Moteur | Idée retenue pour HumbleEngine |
|--------|-------------------------------|
| **Godot** | Node Tree + Scenes instanciables (réutilisabilité) + Signaux |
| **Unity** | Templates réutilisables (≈ Prefabs) |
| **Unreal** | Séparation logique de contrôle / représentation visuelle |
| **Bevy** | Concept de **World** comme conteneur global |

---

## Axe 2 — Comparaison des systèmes UI

| Critère | HTML + CSS | WPF / XAML | Flutter | Qt |
|--------|:---------:|:----------:|:-------:|:--:|
| **Paradigme** | Arbre de balises + règles séparées | Arbre XAML + Data Binding | Tout est Widget | Widgets + Signals & Slots |
| **Pattern architectural** | — | MVVM (natif) | Stateless / Stateful / Provider | MVC / Modèle-Vue |
| **Réactivité** | Via JavaScript | INotifyPropertyChanged | setState / InheritedWidget | Signals |
| **Data Binding** | Absent (natif) | ✅ **Intégré et profond** | Via Provider | Via Modèle/Vue |
| **Langage** | HTML/CSS/JS | XAML + **C#** | Dart | C++ / QML |
| **Plateformes** | Web | Windows | **Toutes** | **Tous OS desktop** |
| **Rendu** | Navigateur | Composition Windows | Pixel par pixel (Skia) | Natif OS |
| **Communication** | Events JS | Commands / INotifyPropertyChanged | setState / callbacks | Signals & Slots |
| **Pertinence pour C#** | ❌ | ✅ **Natif** | ❌ | ❌ |
| **Séparation structure/style** | ✅ Maximale | ✅ XAML / Styles | ❌ (tout dans le Widget) | Partielle |

### Ce qu'on retient de chaque système UI

| Système | Idée retenue pour HumbleEngine |
|---------|-------------------------------|
| **HTML/CSS** | Séparation structure / apparence — les styles restent indépendants de la structure |
| **WPF/XAML** | **MVVM + Data Binding + ResourceDictionaries + Commands** |
| **Flutter** | *Tout est Node* — unifie contenu, style et mise en page dans un seul concept |
| **Qt** | **Signals & Slots** comme système de communication découplé |

---

## Choix d'architecture pour HumbleEngine

### Choix 1 — Architecture du moteur

**→ Node Tree inspiré de Godot, avec Signals inspirés de Qt/Godot**

#### Arguments

**Pour le Node Tree :**
- Architecture naturelle pour une hiérarchie d'éléments UI (fenêtre → panneaux → boutons).
- Intuitive, visible, sérialisable et facile à déboguer.
- S'étend naturellement vers un moteur de jeu : un `Node2D`, un `Node3D` → même principe.
- Implémentable proprement en C# — contrairement à Bevy (Rust) ou Unreal (C++ first).

**Contre l'ECS (Bevy) :**
L'ECS est taillé pour des milliers d'entités homogènes en parallèle. Une UI est hétérogène, peu nombreuse, et profondément imbriquée. La séparation données/logique serait une friction constante.

**Contre le GameObject+Components (Unity) :**
L'empilement de Components est puissant pour les jeux mais moins lisible pour une UI. Un `ButtonNode` est une intention claire ; un `GameObject + ButtonComponent` est une composition moins intuitive.

---

### Choix 2 — Système UI

**→ Architecture inspirée de WPF/XAML, avec le système de Signals de Qt/Godot**

#### Arguments

**Pour WPF comme référence principale :**
- Seul système de la liste **natif C#**.
- **MVVM** est un pattern solide, testé en production, parfaitement adapté à une UI découplée.
- Le **Data Binding** (INotifyPropertyChanged, ObservableCollection) est le mécanisme de réactivité le plus propre pour une UI applicative.
- Les **ResourceDictionaries** et styles implicites donnent un système de thèmes puissant.
- Les **Commands** découplent les actions de l'UI — idéal pour la testabilité.

**Pourquoi pas Flutter comme modèle principal :**
Flutter impose Dart et son propre runtime. L'idée *tout est Widget* et les trois arbres (Widget/Element/RenderObject) sont des complexités justifiées par le cross-platform mobile — pas par une UI desktop C#.

---

## Architecture cible de HumbleEngine

```
HumbleEngine
├── SceneGraph (Node Tree, inspiré Godot)
│   ├── Node de base (cycle de vie : Init, Update, Dispose)
│   ├── Nodes UI (Panel, Button, TextBox, Label…)
│   ├── Nodes Layout (StackLayout, Grid, DockLayout…)
│   └── Nodes composites (instanciables, ≈ Scenes Godot / Prefabs Unity)
│
├── Signal System (inspiré Qt / Godot)
│   ├── Communication découplée entre Nodes
│   └── Connexion Signal → Action (lambda ou méthode nommée)
│
├── Data Binding (inspiré WPF)
│   ├── INotifyPropertyChanged
│   ├── ObservableCollection
│   └── Binding bidirectionnel (View ↔ ViewModel)
│
├── MVVM (inspiré WPF)
│   ├── View = description des Nodes
│   ├── ViewModel = expose les données liées à la View
│   └── Model = données brutes
│
└── Resource System (inspiré WPF / Godot)
    ├── Styles applicables à des types de Nodes
    ├── Ressources globales (couleurs, polices, espacements)
    └── Thèmes swappables au runtime
```

---

## Tableau des inspirations croisées

| Concept HumbleEngine | Inspiré de | Raison du choix |
|---------------------|-----------|-----------------|
| Node Tree | Godot | Hiérarchie naturelle, extensible vers le jeu |
| Signals | Qt / Godot | Communication découplée, éprouvée depuis 1995 |
| Scenes instanciables | Godot / Unity (Prefabs) | Réutilisabilité des composants UI |
| MVVM | WPF | Pattern le plus adapté à une UI C# |
| Data Binding | WPF | Réactivité propre sans couplage fort |
| ObservableCollection | WPF | Listes réactives pour les vues dynamiques |
| Commands | WPF | Actions découplées et testables |
| ResourceDictionary | WPF | Thèmes et styles centralisés |
| Cycle de vie des Nodes | Godot / Unity | `Init` / `Update` / `Dispose` universels |
| World (conteneur global) | Bevy | Registre de tous les Nodes actifs |

---

## Quiz — Questions clés

- Pourquoi le Node Tree est-il préféré à l'ECS pour un moteur UI ?
- Quelle idée de Flutter est retenue dans HumbleEngine, et comment est-elle adaptée ?
- Pourquoi WPF est-il la référence principale pour le système UI, malgré sa limitation Windows-only ?
- Quel système de Qt a directement influencé Godot, et qu'est-ce que HumbleEngine en retient ?
- Dans l'architecture cible, quel composant joue le rôle du SceneTree de Godot ?
- Pourquoi l'ECS de Bevy n'est-il pas retenu, alors qu'il offre de meilleures performances ?
