# Unreal Engine — Fiche de révision

> Axe 1 — Architectures de moteurs de jeu
> Dernière mise à jour : Tour 1 (Vulgarisation) ✅

---

## L'idée centrale

Tout ce qui existe dans le monde de jeu est un **Actor**. Pas une coquille vide — un objet qui a déjà une existence propre dans le Level. On lui attache des **Components** pour enrichir ses comportements, mais contrairement à Unity, certains types d'Actors viennent déjà préconfigurés.

---

## Les concepts fondamentaux

| Concept | Définition courte |
|---------|------------------|
| **Actor** | Tout objet placé dans le Level. A une existence propre (≠ GameObject vide). |
| **Component** | Brique de comportement attachée à un Actor (collision, mesh, mouvement…). |
| **Pawn** | Actor qui peut être possédé et contrôlé (par un joueur ou une IA). |
| **Character** | Pawn spécialisé avec déplacement, collision et mesh intégrés. |
| **Controller** | Entité qui possède et pilote un Pawn. Sépare logique et représentation. |
| **Level** | L'espace de jeu — contient tous les Actors actifs (≈ Scène dans Godot/Unity). |

---

## L'Actor

- Tout ce qui peut être placé dans le Level est un Actor.
- Contrairement au GameObject Unity, il n'est pas vide par défaut.
- Exemples : personnage, lumière, porte, caméra, zone de déclenchement…

---

## Les Components

- Briques de comportement attachées à un Actor, comme dans Unity.
- Différence clé : selon le type d'Actor, certains Components sont déjà préconfigurés.

```
Actor "Joueur" (type : Character)
├── CapsuleComponent      ← collision, toujours présent sur un Character
├── SkeletalMeshComponent ← affiche le personnage animé
├── CharacterMovement     ← déplacements, saut, gravité
└── CameraComponent       ← caméra attachée au personnage
```

---

## La hiérarchie des Actors

Unreal propose une hiérarchie de classes d'Actors spécialisés :

| Type | Rôle |
|---|---|
| **Actor** | La base — tout objet dans le monde |
| **Pawn** | Actor pouvant être possédé et contrôlé |
| **Character** | Pawn avec déplacement, collision et mesh intégrés |
| **Controller** | Possède et pilote un Pawn (joueur ou IA) |

---

## Le concept clé : Controller → Pawn

L'idée unique d'Unreal : **séparer ce qui décide de ce qui bouge**.

```
PlayerController ──possède──▶ Character (Joueur)
AIController     ──possède──▶ Character (Ennemi)
```

- Le **Controller** contient la logique de contrôle (input joueur, comportement IA).
- Le **Pawn/Character** est la représentation physique dans le monde.
- On peut swapper le Controller sans toucher au Pawn — très flexible.

---

## Comparaison avec Godot et Unity

```
Godot          Unity               Unreal
──────         ──────              ──────
Node           GameObject          Actor
(spécialisé)   + Components        + Components (préconfiguré)
Signal         GetComponent<T>()   Delegate / Event Dispatcher
Scene          Scène               Level
```

---

## À retenir absolument

1. **Actor** = tout objet dans le Level, avec une existence propre.
2. **Component** = comportement attaché, souvent préconfiguré selon le type d'Actor.
3. **Pawn / Character** = Actors spécialisés pour les entités contrôlables.
4. **Controller** = séparation claire entre logique de contrôle et représentation physique.
5. **Level** = l'espace de jeu (≈ Scène dans Godot/Unity).

---

## Quiz — Questions clés

- Comment s'appelle tout objet placé dans le monde dans Unreal ?
- Quelle différence entre un Actor Unreal et un GameObject Unity ?
- Qu'est-ce qu'un Pawn ?
- Quel est le rôle du Controller, et pourquoi est-ce une idée forte ?
- Comment s'appelle l'espace de jeu dans Unreal ?

---

*Tour 2 (intermédiaire) : cycle de vie des Actors (BeginPlay, Tick…), Blueprint vs C++, système de possession détaillé, Gameplay Framework, communication via Delegates et Event Dispatchers.*
