# Godot — Vision moins vulgarisée

Ce document approfondit les premiers concepts de Godot vus dans les notes précédentes.

L’objectif est de commencer à comprendre Godot comme un véritable moteur, tout en restant accessible.

---

# Le cœur de Godot : le SceneTree

Le centre du moteur est un arbre de nœuds.

Chaque objet du jeu est généralement un `Node` placé dans une hiérarchie.

Exemple :

```text
Player
├── Sprite2D
├── CollisionShape2D
├── Camera2D
└── AnimationPlayer
```

L’ensemble du jeu est géré par un objet global appelé :

```text
SceneTree
```

Le `SceneTree` :

- possède la racine de la scène,
- parcourt les nœuds,
- appelle les callbacks,
- distribue les inputs,
- gère les scènes,
- gère les groupes et signaux.

---

# Ce qu’est réellement un Node

Un `Node` représente :

- un objet hiérarchique,
- un parent,
- des enfants,
- un cycle de vie,
- une présence dans le SceneTree.

Version très simplifiée :

```csharp
class Node
{
    Node Parent;
    List<Node> Children;

    virtual void _EnterTree() {}
    virtual void _Ready() {}
    virtual void _Process(float delta) {}
    virtual void _ExitTree() {}
}
```

---

# Le cycle de vie des nœuds

## `_EnterTree`

Le nœud entre dans le `SceneTree`.

Le parent existe déjà, mais les enfants ne sont pas encore forcément prêts.

Ordre d’appel :

```text
Parent
→ Child
→ PetitEnfant
```

---

## `_Ready`

Tous les enfants sont prêts.

Ordre inverse :

```text
PetitEnfant
→ Child
→ Parent
```

Cela permet au parent d’utiliser ses enfants en sécurité.

---

## `_Process(delta)`

Appelé à chaque frame.

```text
while (running)
{
    delta = computeDeltaTime();
    root.Process(delta);
}
```

Le `delta` représente le temps écoulé depuis la frame précédente.

---

# `_Process` vs `_PhysicsProcess`

Godot sépare :

## Boucle visuelle

Variable selon les FPS.

Utilise :

```text
_Process(delta)
```

---

## Boucle physique

Fixe.

Par défaut :

```text
60 fois par seconde
```

Utilise :

```text
_PhysicsProcess(delta)
```

Cette séparation permet d’avoir une physique stable indépendamment du framerate.

---

# Node, Node2D et Control

## Node

Objet logique pur.

Pas de position.

Utilisé pour :

- managers,
- logique,
- réseau,
- audio.

---

## Node2D

Ajoute une transform 2D :

- position,
- rotation,
- scale.

Chaque objet possède une transform locale.

La transform globale est calculée via la hiérarchie.

---

## Control

Utilisé pour l’interface utilisateur.

Ajoute :

- anchors,
- margins,
- layout,
- clipping,
- gestion UI souris/clavier.

Les `Control` fonctionnent en coordonnées d’interface, pas en coordonnées monde.

---

# Les transforms hiérarchiques

Chaque objet possède une transform locale.

Exemple :

```text
Player position = (100, 50)
Sword position = (20, 0)
```

La position globale devient :

```text
(120, 50)
```

Concept fondamental :

```text
Global = Parent * Local
```

---

# Les scènes Godot

Une scène Godot est essentiellement :

```text
un sous-arbre sérialisé
```

Exemple :

```text
Player
├── Sprite2D
├── Camera2D
└── CollisionShape2D
```

est enregistré dans un fichier `.tscn`.

Quand Godot charge une scène :

1. il lit le fichier,
2. recrée les nœuds,
3. reconnecte les relations parent/enfant,
4. ajoute le tout au SceneTree.

---

# Les signaux

Les signaux sont un système d’événements.

Exemple :

```gdscript
signal died
```

Puis :

```gdscript
emit_signal("died")
```

Un autre nœud peut écouter l’événement.

Cela réduit fortement le couplage entre systèmes.

---

# Le rendu dans Godot

Le moteur ne dessine pas directement depuis les nœuds.

Le pipeline simplifié est :

1. parcours du SceneTree,
2. génération de commandes de rendu,
3. tri et batching,
4. envoi au GPU.

Exemple de commande :

```text
Draw texture X
at transform Y
with material Z
```

---

# Les serveurs Godot

Godot sépare les systèmes haut niveau et bas niveau.

Exemples :

- RenderingServer
- PhysicsServer
- AudioServer
- NavigationServer

Les nœuds gameplay communiquent avec ces serveurs.

Cela sépare :

- gameplay,
- backend moteur.

---

# Ce que cela implique pour HumbleEngine

Le projet s’oriente désormais vers un moteur UI interactif inspiré de Godot.

Les systèmes prioritaires deviennent :

- SceneTree,
- Control,
- layout,
- rendu UI,
- input,
- événements,
- sérialisation.

L’objectif est de reproduire progressivement l’architecture orientée interface utilisateur de Godot avant d’aborder les systèmes gameplay plus complexes.
