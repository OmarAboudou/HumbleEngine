# Godot — Fiche de révision

> Axe 1 — Architectures de moteurs de jeu
> Dernière mise à jour : Tour 1 (Vulgarisation) ✅

---

## Les 3 idées fondamentales

| Concept | Définition courte |
|---------|------------------|
| **Node** | La brique de base. Chaque Node fait une seule chose (afficher, jouer un son, détecter une collision…). |
| **Scene** | Un arbre de Nodes assemblés, réutilisable comme une brique dans d'autres Scenes (instanciation). |
| **Signal** | Système de messages qui permet aux Nodes de communiquer sans se coupler directement. |

---

## Le Node

- L'atome de Godot : tout est un Node.
- Un Node seul est limité. Assemblés en arbre, les Nodes forment quelque chose de puissant.
- Exemples : `Sprite2D`, `CollisionShape2D`, `AudioStreamPlayer`, `Camera2D`…

---

## La Scene

- Un arbre de Nodes organisés pour former un objet cohérent.
- **Instanciation** : une Scene peut être utilisée comme un Node dans une autre Scene.

```
Scene "Joueur"
├── Sprite2D          ← affiche le personnage
├── CollisionShape2D  ← gère les collisions
└── AudioStreamPlayer ← joue les sons

Scene "Niveau"
├── TileMap           ← le décor
├── Joueur (Scene)    ← instanciée comme un bloc
└── Ennemi (Scene)    ← instanciée comme un bloc
```

---

## Le SceneTree

- L'arbre global de tous les Nodes actifs, maintenu par le moteur au runtime.
- Il met à jour chaque Node à chaque frame, transmet les événements (clavier, souris…) et gère l'ordre d'exécution.

```
SceneTree
└── Scene racine
    ├── Node A
    │   ├── Node A1
    │   └── Node A2
    └── Node B  (= autre Scene instanciée)
        ├── Node B1
        └── Node B2
```

---

## Les Signaux

- Système de notifications découplé : l'émetteur ne sait pas qui écoute, l'écouteur ne dépend pas de l'émetteur.
- Exemple : un bouton émet `pressed` → ton code réagit sans que le bouton sache ce qui se passe.
- Favorise la **maintenabilité** et la **modularité**.

---

## À retenir absolument

1. **Tout est Node** dans Godot.
2. Une **Scene** = un arbre de Nodes réutilisable (instanciation).
3. Les **Signaux** = communication sans couplage fort.
4. Le **SceneTree** = l'arbre global qui tourne à l'exécution.

---

## Quiz — Questions clés

- Quelle est la brique de base de Godot ?
- Quelle est la différence entre un Node et une Scene ?
- Peut-on utiliser une Scene comme Node dans une autre Scene ?
- Quel est le rôle du SceneTree ?
- Pourquoi utilise-t-on des Signaux plutôt qu'un appel direct entre Nodes ?

---

*Tour 2 (intermédiaire) : terminologie exacte, cycle de vie des Nodes, types de Nodes natifs, GDScript vs C#, communication avancée entre Scenes.*
