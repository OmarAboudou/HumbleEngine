# HumbleEngine — Documentation d'apprentissage

Cette documentation contient les traces de mon apprentissage dans le cadre du projet **HumbleEngine**.

## Objectif du projet

Acquérir suffisamment de connaissances sur les moteurs de jeu et les moteurs d'application pour concevoir et implémenter mon propre moteur en C#.

**Scope initial visé** : un moteur capable de faire tourner des applications de type UI interactif (logiciels, interfaces graphiques). L'architecture exacte sera choisie après étude comparative des systèmes existants.

**Ambition long terme** : extensible vers un moteur de jeu complet.

## Structure de la documentation

```
docs/
├── README.md               ← Ce fichier
├── roadmap.md              ← Plan de route complet de l'apprentissage
└── concepts/
    ├── axe1-moteurs/       ← Architectures des moteurs de jeu
    │   ├── godot_v1.md     ← Tour 1 : Vulgarisation
    │   ├── godot_v2.md     ← Tour 2 : Intermédiaire
    │   ├── godot_v3.md     ← Tour 3 : Technique
    │   ├── unity_v1.md
    │   ├── unity_v2.md
    │   ├── unity_v3.md
    │   ├── unreal_v1.md
    │   ├── unreal_v2.md
    │   ├── unreal_v3.md
    │   ├── bevy_v1.md
    │   ├── bevy_v2.md
    │   └── bevy_v3.md
    └── axe2-ui/            ← Systèmes UI / frameworks applicatifs
        ├── html-css_v1.md
        ├── html-css_v2.md
        ├── html-css_v3.md
        ├── wpf-xaml_v1.md
        ├── wpf-xaml_v2.md
        ├── wpf-xaml_v3.md
        ├── flutter_v1.md
        ├── flutter_v2.md
        ├── flutter_v3.md
        ├── qt_v1.md
        ├── qt_v2.md
        └── qt_v3.md
```

## Approche pédagogique

L'apprentissage suit une **approche en spirale** :

1. Tous les sujets sont d'abord abordés en **vulgarisation** (`_v1`)
2. Puis revisités de manière de plus en plus **précise et technique** (`_v2`, `_v3`)
3. Chaque tour donne lieu à une **nouvelle fiche**, la version précédente est conservée

Les discussions sont organisées **par tour** : tous les sujets du Tour 1 dans la même discussion, puis tous les sujets du Tour 2, etc.

## Langage cible

**C#** — avec une attention particulière aux patterns orientés moteur (boucle principale, scene graph, système d'événements).
