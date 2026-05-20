# Bevy — Fiche de révision

> Axe 1 — Architectures de moteurs de jeu
> Dernière mise à jour : Tour 1 (Vulgarisation) ✅

---

## L'idée centrale

Oublie les objets. Bevy sépare radicalement les **données** de la **logique**. D'un côté des identifiants auxquels on attache des paquets de données, de l'autre des fonctions qui traitent ces données. Le moteur fait le lien automatiquement. C'est l'**ECS — Entity Component System**.

---

## Les 3 briques fondamentales

| Concept | Définition courte |
|---------|------------------|
| **Entity** | Un simple identifiant numérique. Pas de données, pas de logique. |
| **Component** | Un paquet de données pures attaché à une Entity (pas de logique). |
| **System** | Une fonction qui traite des Components. Pas de données propres. |

---

## L'Entity

- Juste un numéro unique. Rien de plus.
- Seule, elle ne signifie rien — c'est les Components attachés qui lui donnent un sens.

```
Entity 0  →  Position, Vitesse, Santé, Sprite   (= le joueur)
Entity 1  →  Position, Sprite                   (= une lampe)
Entity 2  →  Position, Vitesse, Santé           (= un ennemi)
```

---

## Le Component

- Uniquement des **données pures** — aucune logique, aucune méthode.
- On attache autant de Components que nécessaire à une Entity.

```
Position  { x: 10.0, y: 5.0 }
Vitesse   { x: 1.0,  y: 0.0 }
Santé     { valeur: 100 }
Sprite    { image: "joueur.png" }
```

---

## Le System

- Une **fonction pure** qui exprime une requête : "donne-moi toutes les Entities qui ont tel et tel Component".
- Le moteur trouve automatiquement les bonnes Entities et les passe au System.

```
System "déplacement" :
→ cherche toutes les Entities avec Position ET Vitesse
→ met à jour Position en fonction de Vitesse

System "rendu" :
→ cherche toutes les Entities avec Position ET Sprite
→ les affiche à l'écran
```

---

## La séparation données / logique

C'est la rupture fondamentale avec Godot, Unity et Unreal :

```
Unity / Godot / Unreal          Bevy ECS
──────────────────────          ──────────────────────
Objet "Joueur"                  Entity 0
├── données                     ├── Component Position
└── logique (scripts)           ├── Component Vitesse
                                └── Component Santé

                                System "déplacement"
                                → traite toutes les Entities
                                  avec Position + Vitesse
```

Le System ne sait pas qu'il s'agit d'un "joueur" — il voit juste des Entities avec les bons Components. **Les données et la logique ne se connaissent pas.**

---

## Ce que ça permet

- **Modularité** : ajouter un Component à une Entity suffit pour qu'un System la prenne en charge.
- **Réutilisabilité** : un même System gère joueur ET ennemis s'ils ont les mêmes Components.
- **Performance** : le moteur peut paralléliser les Systems automatiquement.

---

## Comparaison des quatre moteurs

```
Godot    → tout est Node, organisé en arbre, communication par Signaux
Unity    → GameObject vide + Components empilés (données + logique mélangées)
Unreal   → Actor avec existence propre + hiérarchie de classes spécialisées
Bevy     → Entities (ids) + Components (données) + Systems (logique) — tout séparé
```

---

## À retenir absolument

1. **Entity** = un simple identifiant numérique. Rien de plus.
2. **Component** = des données pures, sans logique.
3. **System** = de la logique pure, sans données propres.
4. Le moteur connecte automatiquement les Systems aux Entities qui ont les bons Components.
5. Données et logique sont **complètement séparées** — c'est l'inverse des approches objet.

---

## Quiz — Questions clés

- Qu'est-ce qu'une Entity dans Bevy ?
- Quelle est la différence entre un Component Bevy et un Component Unity ?
- Qu'est-ce qu'un System, et comment sait-il sur quelles Entities agir ?
- Si un ennemi et un joueur ont les mêmes Components, que se passe-t-il ?
- Quels sont les trois avantages principaux de l'ECS ?

---

*Tour 2 (intermédiaire) : World et App, cycle de vie des Systems (Startup, Update…), Queries et Filters, Resources, Events, schedules et ordonnancement des Systems.*
