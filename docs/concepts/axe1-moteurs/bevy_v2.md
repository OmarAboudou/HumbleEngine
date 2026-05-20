# Bevy — Fiche de révision

> Axe 1 — Architectures de moteurs de jeu
> Dernière mise à jour : Tour 2 (Intermédiaire) ✅

---

## Ce qu'on a vu au Tour 1 (rappel)

| Concept | Définition courte |
|---------|------------------|
| **Entity** | Un simple identifiant numérique. Rien de plus. |
| **Component** | Des données pures, sans logique. |
| **System** | De la logique pure, sans données propres. |
| **ECS** | Données et logique complètement séparées. |

---

## 1. World et App

### Le World

Le **World** est le conteneur global de tout ce qui existe : toutes les Entities, tous leurs Components, toutes les Resources. Pas de hiérarchie — un sac plat.

```
World
├── Entity 0  →  Position, Vitesse, Santé
├── Entity 1  →  Position, Sprite
├── Entity 2  →  Position, Vitesse
└── Resources →  ScoreGlobal, HeureJournee...
```

### L'App

Le point d'entrée du jeu. On y déclare tout : plugins, Systems, Events.

```rust
fn main() {
    App::new()
        .add_plugins(DefaultPlugins)
        .add_systems(Startup, setup)
        .add_systems(Update, deplacement)
        .run();
}
```

---

## 2. Cycle de vie des Systems

Les Systems sont ordonnés dans des **Schedules**.

| Schedule | Quand |
|----------|-------|
| `Startup` | Une seule fois, au démarrage |
| `Update` | Chaque frame |
| `FixedUpdate` | À intervalle fixe (physique) |
| `PostUpdate` | Après Update, pour la synchronisation |

```rust
App::new()
    .add_systems(Startup, creer_joueur)
    .add_systems(Update, (deplacement, afficher_score))
    .add_systems(FixedUpdate, physique)
    .run();
```

Équivalents Godot : `Startup` ≈ `_ready()`, `Update` ≈ `_process()`, `FixedUpdate` ≈ `_physics_process()`.

---

## 3. Queries et Filters

### Query basique

Un System déclare ce qu'il veut — le moteur fournit automatiquement les Entities correspondantes.

```rust
fn deplacement(mut query: Query<(&mut Position, &Vitesse)>) {
    for (mut pos, vel) in query.iter_mut() {
        pos.x += vel.x;
        pos.y += vel.y;
    }
}
```

### Filters

```rust
// With<T>     → garde seulement les Entities QUI ONT T
fn deplacement_joueur(query: Query<(&mut Position, &Vitesse), With<Joueur>>) { ... }

// Without<T>  → garde seulement les Entities QUI N'ONT PAS T
fn deplacement_ennemis(query: Query<(&mut Position, &Vitesse), Without<Joueur>>) { ... }
```

| Filter | Ce qu'il fait |
|--------|--------------|
| `With<T>` | Garde seulement les Entities qui **ont** T |
| `Without<T>` | Garde seulement les Entities qui **n'ont pas** T |
| `Changed<T>` | Garde seulement les Entities dont T a changé depuis la dernière frame |

⚠️ **Piège classique** : `Without<Joueur>` exclut les joueurs, il n'en cherche pas. Ne pas confondre avec `With<Joueur>`.

---

## 4. Resources

Une **Resource** est une donnée globale unique — une seule instance dans tout le World.

```rust
#[derive(Resource)]
struct ScoreGlobal { valeur: u32 }

App::new()
    .insert_resource(ScoreGlobal { valeur: 0 })
    ...

fn afficher_score(score: Res<ScoreGlobal>) {
    println!("Score : {}", score.valeur);
}

fn ajouter_points(mut score: ResMut<ScoreGlobal>) {
    score.valeur += 10;
}
```

### Resource vs Component

| | Component | Resource |
|--|-----------|----------|
| **Instances** | Une par Entity | Une seule dans le World |
| **Lié à** | Une Entity | Rien — c'est global |
| **Cas d'usage** | Vitesse d'un ennemi | Score global, heure du jeu |

### Équivalents dans les autres moteurs

```
Godot Autoload    →  Resource Bevy
Unity static      →  Resource Bevy
Unreal GameState  →  Resource Bevy (partiellement)
```

---

## 5. Events

Les **Events** permettent à un System d'envoyer un message qu'un autre System peut lire — sans que les deux se connaissent.

```rust
#[derive(Event)]
struct JoueurMort { position: Vec2 }

// Émission
fn detecter_mort(mut events: EventWriter<JoueurMort>) {
    events.send(JoueurMort { position: Vec2::ZERO });
}

// Réception
fn sur_joueur_mort(mut events: EventReader<JoueurMort>) {
    for event in events.read() {
        println!("Joueur mort en {:?}", event.position);
    }
}
```

### Comparaison avec les autres moteurs

```
Godot Signal       →  Bevy Event
Unity UnityEvent   →  Bevy Event
Unreal Delegate    →  Bevy Event
```

Différence clé : dans Bevy, émetteur et récepteur sont des Systems entièrement séparés. Personne ne connaît personne.

---

## À retenir absolument

1. **World** = conteneur global plat (pas de hiérarchie). **App** = point d'entrée.
2. **Schedules** : `Startup` (une fois) / `Update` (frame) / `FixedUpdate` (physique).
3. **Query** = sélection d'Entities par combinaison de Components.
4. `With<T>` = inclusion. `Without<T>` = exclusion. Ne pas confondre.
5. **Resource** = donnée globale unique (≠ Component lié à une Entity).
6. **Event** = communication découplée entre Systems.

---

## Quiz — Questions clés

- Quelle est la différence entre un Component et une Resource ?
- À quoi sert le Schedule `Startup` ?
- Que fait `Query<&Position, Without<Joueur>>` ?
- Pourquoi les Events Bevy sont-ils plus découplés que les Signaux Godot ?
- Cite un cas d'usage concret pour une Resource.

---

*Tour 3 (technique) : parallélisme automatique des Systems, gestion mémoire ECS, archetypes, relations entre Entities, Bevy Scenes et assets.*
