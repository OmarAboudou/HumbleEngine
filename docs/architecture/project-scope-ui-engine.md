# Nouvelle orientation du projet

Le projet évolue désormais vers la création d’un moteur UI interactif en C# inspiré de Godot, plutôt qu’un moteur de jeu complet.

## Objectif

L’objectif principal devient :

- comprendre l’architecture interne d’un moteur moderne,
- reproduire les systèmes orientés interface utilisateur de Godot,
- construire progressivement un moteur capable d’alimenter un éditeur et des interfaces interactives.

Le but n’est plus, dans un premier temps, de gérer un monde de jeu complet avec physique, caméra ou gameplay temps réel.

---

# Pourquoi ce changement de scope

Un moteur de jeu complet introduit très tôt de nombreux systèmes complexes :

- physique,
- collisions,
- audio spatialisé,
- caméras,
- monde 2D/3D,
- gameplay temps réel,
- animation avancée.

Réduire la portée du projet permet :

- d’avancer plus vite,
- de mieux comprendre chaque couche du moteur,
- d’obtenir rapidement un résultat utilisable,
- de construire un éditeur plus tôt,
- de garder une architecture propre et progressive.

---

# Systèmes prioritaires

Le moteur se concentre désormais principalement sur les systèmes UI de Godot.

## Architecture scène

- SceneTree
- Node
- Control
- Viewport
- cycle de vie des nœuds

## Interface utilisateur

- layout
- VBoxContainer / HBoxContainer
- anchors
- margins
- clipping
- hiérarchie de contrôles

## Rendu

- rendu 2D orienté interface
- rectangles
- texte
- images
- batching simple

## Interaction

- souris
- clavier
- focus
- événements
- signaux

## Ressources

- sérialisation de scènes
- chargement de ressources
- thèmes UI

---

# Fonctionnalités explicitement reportées

Les systèmes suivants ne sont plus prioritaires dans la première phase du moteur :

- physique
- collisions gameplay
- caméra de jeu
- monde 2D complexe
- audio spatialisé
- navigation
- gameplay temps réel
- renderer 3D

Ces systèmes pourront être ajoutés plus tard une fois la base UI stabilisée.

---

# Architecture cible simplifiée

```text
Application
├── Window
├── SceneTree
│   └── RootControl
├── InputSystem
├── LayoutSystem
└── Renderer2D
```

Hiérarchie UI :

```text
Control
├── Panel
├── Label
├── Button
└── Container
    ├── VBoxContainer
    └── HBoxContainer
```

---

# Séparation documentation

Le dossier `docs/learning/` reste dédié aux fiches pédagogiques et aux résumés d’apprentissage.

Les réflexions liées à l’architecture, aux choix techniques et à la conception du moteur sont désormais placées dans :

```text
docs/architecture/
```

Cette séparation permet de distinguer :

- l’apprentissage des concepts,
- la conception réelle du moteur.

---

# Vision long terme

L’objectif long terme reste d’apprendre les principes de conception des moteurs modernes.

Cette approche permet :

1. d’apprendre progressivement,
2. de construire une architecture propre,
3. d’obtenir rapidement un moteur utilisable,
4. de créer un éditeur tôt dans le projet,
5. d’ajouter ensuite les systèmes gameplay de manière incrémentale.
