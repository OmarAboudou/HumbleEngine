# Godot — Fiche de révision

> Axe 1 — Architectures de moteurs de jeu
> Dernière mise à jour : Tour 3 (Technique) ✅

---

## Ce qu'on a vu au Tour 2 (rappel)

| Concept | Définition courte |
|---------|------------------|
| `_ready()` | Une fois, au démarrage. |
| `_process(delta)` | Chaque frame. Multiplier par delta pour indépendance framerate. |
| `_physics_process(delta)` | Chaque pas physique fixe. |
| `Node2D` / `Control` | Monde 2D vs espace UI. |
| Signaux | Communication inter-Scenes découplée. |
| Autoload | Singleton global — puissant mais à ne pas abuser. |

---

## 1. SceneTree avancé et Groupes

### Le SceneTree comme API

Le SceneTree n'est pas seulement l'arbre d'exécution — c'est aussi une API manipulable depuis n'importe quel script.

```gdscript
get_tree().change_scene_to_file("res://scenes/GameOver.tscn")
get_tree().paused = true
get_tree().quit()
```

### Les Groupes

Un **Groupe** est un tag attaché à un Node. Le SceneTree permet ensuite d'opérer sur tous les Nodes du groupe d'un coup.

```gdscript
# Dans _ready() de chaque Node concerné :
add_to_group("ennemis")

# Depuis n'importe où — appel broadcast :
get_tree().call_group("ennemis", "sur_joueur_mort")

# Récupérer la liste :
var ennemis = get_tree().get_nodes_in_group("ennemis")
```

### Groupes vs Signaux

| Situation | Outil |
|-----------|-------|
| Un émetteur → récepteurs connus à l'avance | Signal |
| Broadcast vers une catégorie de Nodes | Groupe |

---

## 2. Les Servers

### Architecture en deux couches

```
Couche haute  →  Nodes     (interface confortable, GDScript)
                     ↕
Couche basse  →  Servers   (moteurs internes, RIDs)
```

Les Servers sont les moteurs réels de Godot. Ils ne manipulent pas des Nodes mais des **RIDs** (Resource IDs) — de simples identifiants numériques.

| Server | Responsabilité |
|--------|---------------|
| `RenderingServer` | Tout ce qui s'affiche |
| `PhysicsServer2D/3D` | Toute la physique |
| `AudioServer` | Tout le son |

### Ce que fait un Node en réalité

```gdscript
# Ce que tu écris :
var sprite = Sprite2D.new()

# Ce que Godot fait en interne :
var rid = RenderingServer.canvas_item_create()
# Le Node est une enveloppe autour du RID
```

### Accès direct aux Servers

Contourne la couche Node pour des situations à haute performance :

```gdscript
# 10 000 sprites via RenderingServer — beaucoup plus rapide que 10 000 Nodes
for i in 10000:
    var rid = RenderingServer.canvas_item_create()
    RenderingServer.canvas_item_set_parent(rid, get_canvas_item())
```

**Règle d'usage** : Nodes pour 95% des cas. Servers pour les situations rares où chaque milliseconde compte.

---

## 3. Multithreading

### Le problème

Godot tourne sur un seul thread par défaut. Une opération longue freeze tout le jeu.

### Thread

```gdscript
var thread = Thread.new()

func _ready():
    thread.start(calcul_long)

func calcul_long():
    var resultat = effectuer_calcul_lourd()
    call_deferred("mettre_a_jour_ui", resultat)

func mettre_a_jour_ui(valeur):
    $Label.text = str(valeur)  # ✅ thread principal

func _exit_tree():
    thread.wait_to_finish()  # toujours attendre la fin
```

### Mutex

Verrou qui garantit qu'un seul thread accède à une ressource à la fois.

```gdscript
var mutex = Mutex.new()
var score = 0

func ajouter_points(valeur):
    mutex.lock()
    score += valeur
    mutex.unlock()
```

### Règle d'or

> **On ne touche jamais aux Nodes depuis un thread secondaire.**

Les Nodes ne sont pas thread-safe. Utiliser `call_deferred()` pour reporter toute modification de scène sur le thread principal.

---

## 4. Optimisations

### Profiler — diagnostiquer avant d'optimiser

Ne jamais optimiser à l'aveugle. Le Profiler mesure exactement où le temps est dépensé par frame.

```
Frame budget à 60fps : ~16ms
  _process() total    →  8ms
    └── IA ennemis    →  6ms   ← le problème est ici
  Rendu               →  5ms
  Physique            →  2ms
```

### Techniques clés

**Visibility notifiers — stopper ce qui n'est pas visible**

```gdscript
func _on_invisible():
    set_process(false)  # stoppe _process() hors écran

func _on_visible():
    set_process(true)
```

**Cache des références — éviter GetNode dans _process()**

```gdscript
# Mauvais — recherche dans l'arbre à chaque frame
func _process(delta):
    $Sprite2D.visible = false

# Bon — référence en cache dans _ready()
@onready var sprite = $Sprite2D

func _process(delta):
    sprite.visible = false
```

**Object Pooling — recycler plutôt que recréer**

Instancier et détruire des Nodes est coûteux (allocation + désallocation + initialisation + fragmentation mémoire). Pour les objets fréquents (balles, particules, ennemis) : on désactive et réactive plutôt que de détruire et recréer.

```gdscript
var pool = []  # réservoir de Nodes prêts à l'emploi
```

---

## 5. Export

### Pipeline

```
Projet Godot
    ↓  export (via Export Templates)
├── Windows  →  MonJeu.exe
├── Linux    →  MonJeu.x86_64
├── macOS    →  MonJeu.app
├── Android  →  MonJeu.apk
└── Web      →  MonJeu.html + .wasm
```

### Ce que fait l'export

| Étape | Description |
|-------|------------|
| **PCK** | Toutes les ressources empaquetées en un fichier `.pck` |
| **Stripping** | Suppression de l'éditeur — binaire final plus léger |
| **Optimisations** | Textures compressées, scripts en bytecode, scènes en binaire |

**Export Templates** : binaires précompilés officiels pour chaque plateforme, téléchargés une seule fois depuis l'éditeur.

---

## À retenir absolument

1. **Groupes** = tags sur les Nodes pour des opérations broadcast via `call_group()`.
2. **Servers** = moteurs internes bas niveau. Les Nodes sont des enveloppes au-dessus.
3. **Threads** = parallélisme. Jamais de modification de Node hors thread principal — utiliser `call_deferred()`.
4. **Mutex** = verrou pour protéger les données partagées entre threads.
5. **Profiler d'abord** — toujours mesurer avant d'optimiser.
6. **Pooling** = recycler les Nodes coûteux à instancier fréquemment.
7. **Export Templates** = prérequis pour produire un exécutable par plateforme.

---

## Quiz — Questions clés

- Comment appeler une méthode sur tous les Nodes d'un groupe ?
- Qu'est-ce qu'un RID dans le contexte des Servers Godot ?
- Pourquoi ne peut-on pas modifier un Node depuis un thread secondaire ?
- Qu'est-ce que `call_deferred()` résout exactement ?
- Quelle est la première étape avant toute optimisation de performance ?
- Pourquoi le pooling est-il plus performant que créer/détruire des Nodes ?

---

*Synthèse finale : comparaison de toutes les architectures, choix argumenté pour HumbleEngine.*
