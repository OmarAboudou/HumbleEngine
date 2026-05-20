# Godot — Fiche de révision

> Axe 1 — Architectures de moteurs de jeu
> Dernière mise à jour : Tour 2 (Intermédiaire) ✅

---

## Ce qu'on a vu au Tour 1 (rappel)

| Concept | Définition courte |
|---------|------------------|
| **Node** | La brique de base. Chaque Node fait une seule chose. |
| **Scene** | Un arbre de Nodes réutilisable (instanciation). |
| **Signal** | Communication découplée entre Nodes. |
| **SceneTree** | L'arbre global de tous les Nodes actifs au runtime. |

---

## 1. Cycle de vie des Nodes

### Les méthodes fondamentales

```
Node entre dans le SceneTree
        ↓
    _ready()              ← une seule fois, quand le Node est prêt
        ↓
    [chaque frame]
    _process(delta)       ← chaque frame (60/s par défaut)
    _physics_process(delta)  ← chaque pas physique fixe
```

| Méthode | Quand | Pour quoi |
|--------|-------|-----------|
| `_ready()` | Une fois, au démarrage | Initialiser des variables, connecter des Signaux |
| `_process(delta)` | Chaque frame | Logique de jeu (input, animations, caméra) |
| `_physics_process(delta)` | Chaque pas physique fixe | Mouvement, forces, collisions |

### Le paramètre `delta`

`delta` = temps écoulé depuis la dernière frame (en secondes).

```gdscript
func _process(delta):
    position.x += 200 * delta  # 200 pixels/seconde, indépendant du framerate
```

Sans `delta`, la vitesse dépendrait du framerate — comportement non déterministe.

---

## 2. Hiérarchie des types de Nodes natifs

```
Node                        ← la base abstraite
├── Node2D                  ← tout ce qui a une position dans un monde 2D
│   ├── Sprite2D            ← affiche une image
│   ├── CollisionShape2D    ← forme de collision
│   ├── Camera2D            ← caméra 2D
│   └── CharacterBody2D     ← corps physique contrôlable
├── Node3D                  ← tout ce qui a une position dans un monde 3D
│   ├── MeshInstance3D      ← affiche un mesh 3D
│   ├── Camera3D
│   └── CharacterBody3D
├── Control                 ← éléments UI (espace ancres/marges, pas monde 2D)
│   ├── Button
│   ├── Label
│   └── TextEdit
└── AudioStreamPlayer       ← joue un son (sans position spatiale)
```

### Règle d'or

- `Node2D` → position dans le **monde** 2D
- `Control` → position dans l'**espace UI** (ancres, marges, container)
- `Node` seul → logique pure, pas de représentation visuelle

---

## 3. GDScript vs C\#

| Critère | GDScript | C# |
|--------|----------|----|
| **Syntaxe** | Python-like, très concis | C# standard |
| **Intégration** | Native Godot | Via Mono/.NET |
| **Performance** | Bonne pour la majorité des jeux | Meilleure pour calculs intensifs |
| **Cas d'usage** | Prototypage rapide, petits/moyens jeux | Projets larges, devs venant de Unity |

**GDScript :**
```gdscript
extends CharacterBody2D

var vitesse = 200.0

func _physics_process(delta):
    var direction = Input.get_axis("ui_left", "ui_right")
    velocity.x = direction * vitesse
    move_and_slide()
```

**C# équivalent :**
```csharp
using Godot;

public partial class Joueur : CharacterBody2D
{
    private float vitesse = 200.0f;

    public override void _PhysicsProcess(double delta)
    {
        float direction = Input.GetAxis("ui_left", "ui_right");
        Velocity = new Vector2(direction * vitesse, Velocity.Y);
        MoveAndSlide();
    }
}
```

---

## 4. Communication avancée entre Scenes

### 3 méthodes, 3 niveaux de couplage

| Méthode | Couplage | Cas d'usage |
|--------|----------|-------------|
| Accès direct `$` | Fort | Parent → son propre enfant |
| Signaux | Faible | Communication entre Scenes non liées |
| Autoload | Global | État partagé dans tout le projet |

### Méthode 1 — Accès direct

```gdscript
$Sprite2D.visible = false
get_node("AudioStreamPlayer").play()
```

✅ Simple. ⚠️ Acceptable uniquement d'un parent vers son propre enfant.

### Méthode 2 — Signaux

```gdscript
# Émetteur
signal sante_changee(nouvelle_valeur)
emit_signal("sante_changee", 80)

# Écouteur
func _ready():
    $NodeA.sante_changee.connect(_sur_sante_changee)

func _sur_sante_changee(valeur):
    $Label.text = str(valeur)
```

✅ Découplé. ✅ Recommandé pour la communication inter-Scenes.

### Méthode 3 — Autoload (Singleton global)

Un Node chargé au démarrage, accessible depuis n'importe où.

```
Autoloads
├── GameManager   ← score, état global du jeu
└── AudioManager  ← gestion centralisée du son
```

```gdscript
GameManager.score += 10
AudioManager.play("musique_victoire")
```

✅ Pratique pour l'état global. ⚠️ À ne pas abuser — un état global est difficile à tracer et déboguer.

---

## À retenir absolument

1. `_ready()` → une fois. `_process()` → chaque frame. `_physics_process()` → pas physique fixe.
2. Toujours multiplier par `delta` pour une vitesse indépendante du framerate.
3. `Node2D` = monde 2D. `Control` = espace UI. `Node` = logique pure.
4. GDScript pour la rapidité, C# pour les projets larges ou les devs .NET.
5. Communication : accès direct (parent→enfant) / Signaux (inter-Scenes) / Autoload (global).

---

## Quiz — Questions clés

- Dans quel ordre sont appelées `_ready()` et `_process()` ? Combien de fois chacune ?
- Pourquoi multiplier une vitesse par `delta` ?
- Quelle différence entre `Node2D` et `Control` ?
- Quand utiliser les Signaux plutôt que l'accès direct via `$` ?
- Qu'est-ce qu'un Autoload et quel est son risque principal ?

---

*Tour 3 (technique) : Server/RenderingServer, groupes, SceneTree avancé, multithreading, optimisations, export.*
