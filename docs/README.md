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
    │   ├── godot.md
    │   ├── unity.md
    │   ├── unreal.md
    │   └── bevy.md
    └── axe2-ui/            ← Systèmes UI / frameworks applicatifs
        ├── html-css.md
        ├── wpf-xaml.md
        ├── flutter.md
        └── qt.md
```

## Approche pédagogique

L'apprentissage suit une **approche en spirale** :

1. Tous les sujets sont d'abord abordés en **vulgarisation**
2. Puis revisités de manière de plus en plus **précise et technique**
3. Chaque fiche est mise à jour au fur et à mesure des itérations

## Langage cible

**C#** — avec une attention particulière aux patterns orientés moteur (boucle principale, scene graph, système d'événements).
