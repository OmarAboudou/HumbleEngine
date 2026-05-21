# Bevy — Fiche de révision

> Axe 1 — Architectures de moteurs de jeu
> Dernière mise à jour : Tour 3 (Technique) ✅

---

## Ce qu'on a vu au Tour 2 (rappel)

| Concept | Définition courte |
|---------|------------------|
| **World** | Conteneur global plat — toutes les Entities, Components, Resources. |
| **App** | Point d'entrée — déclare plugins, Systems, Events. |
| **Schedules** | `Startup` (une fois) / `Update` (frame) / `FixedUpdate` (physique). |
| **Query** | Sélection d'Entities par combinaison de Components. |
| `With<T>` / `Without<T>` | Inclusion / exclusion dans une Query. |
| **Resource** | Donnée globale unique (≠ Component lié à une Entity). |
| **Event** | Communication découplée entre Systems. |

---

## 1. Parallélisme automatique des Systems

### La règle

Deux Systems peuvent tourner en parallèle s'ils n'accèdent pas aux mêmes données en écriture.

```
System A  →  lit Position, écrit Vitesse
System B  →  lit Position, écrit Santé
System C  →  écrit Position

A et B    →  parallèles ✅  (ils écrivent des Components différents)
A et C    →  séquentiels ⛔  (C écrit Position que A lit)
```

Bevy construit ce graphe de dépendances automatiquement.

### Ordonner explicitement quand nécessaire

```rust
App::new()
    .add_systems(Update, (
        appliquer_input,
        deplacer_joueur,
        detecter_collisions,
    ).chain())  // exécutés dans cet ordre, séquentiellement
```

Sans `.chain()`, Bevy décide lui-même de l'ordre et du parallélisme.

---

## 2. Archetypes et gestion mémoire

### Le principe

Bevy regroupe les Entities qui ont **exactement la même combinaison de Components** dans un même bloc mémoire contigu : un **Archetype**.

```
Archetype A : [Position + Vitesse + Santé]
  Entity 0 : [pos(1,2) | vel(1,0) | hp(100)]
  Entity 2 : [pos(5,3) | vel(0,1) | hp(80) ]

Archetype B : [Position + Sprite]
  Entity 1 : [pos(3,1) | sprite("lampe")]
  Entity 4 : [pos(7,2) | sprite("arbre")]
```

Quand un System traite toutes les Entities avec `Position + Vitesse`, il lit un **bloc mémoire contigu** — cache friendly, zéro saut.

### Migration d'Archetype

Ajouter un Component à une Entity **change son Archetype** — elle migre vers un nouveau bloc mémoire.

```
Entity 0 : [Position + Vitesse]         ← Archetype A
    ↓  add_component(Santé)
Entity 0 : [Position + Vitesse + Santé] ← Archetype B  (migration)
```

Opération coûteuse — à éviter en boucle serrée.

---

## 3. Relations entre Entities

### Le problème

Dans l'ECS pur, les Entities ne se connaissent pas. La hiérarchie parent/enfant doit être exprimée autrement.

### La solution — Components de relation

```rust
// Spawner un enfant attaché à un parent
commands.spawn((
    Transform::default(),
    Parent(parent_entity),   // relation exprimée comme un Component
));
```

### Hiérarchie de transforms

```rust
let parent = commands.spawn(SpatialBundle::default()).id();
let enfant = commands.spawn(SpatialBundle::default()).id();

commands.entity(parent).push_children(&[enfant]);
// Bevy propage automatiquement le transform global parent + enfant
```

---

## 4. Assets et Scenes

### Chargement asynchrone

```rust
fn setup(mut commands: Commands, asset_server: Res<AssetServer>) {
    let texture: Handle<Image> = asset_server.load("sprites/joueur.png");
    // Handle retourné immédiatement — chargement en arrière-plan

    commands.spawn(SpriteBundle {
        texture,  // affiché quand le chargement sera terminé
        ..default()
    });
}
```

### Vérifier l'état de chargement

```rust
match asset_server.get_load_state(&handle.0) {
    LoadState::Loading => println!("En cours..."),
    LoadState::Loaded  => println!("Prêt !"),
    LoadState::Failed  => println!("Erreur"),
    _                  => {}
}
```

### Bevy Scenes

Snapshot sérialisable d'un ensemble d'Entities et leurs Components.

```rust
let scene = DynamicScene::from_world(&world);
let serialized = scene.serialize_ron(&type_registry).unwrap();
// → fichier texte lisible, rechargeable
```

Utile pour les niveaux, les sauvegardes, l'éditeur de scènes.

---

## 5. Optimisations spécifiques à l'ECS

### Change Detection

```rust
fn sur_changement(
    query: Query<&Transform, Changed<Transform>>
    // seulement les Entities dont Transform a changé cette frame
) {
    for transform in &query { /* traitement ciblé */ }
}
```

### Run Conditions

```rust
App::new()
    .add_systems(Update,
        gerer_pause.run_if(in_state(AppState::EnJeu))
    )
```

### States — gérer les états globaux

```rust
#[derive(States, Default, Clone, PartialEq, Eq, Hash, Debug)]
enum AppState { #[default] Menu, EnJeu, Pause, GameOver }

App::new()
    .add_state::<AppState>()
    .add_systems(OnEnter(AppState::EnJeu), setup_jeu)   // hook d'entrée
    .add_systems(OnExit(AppState::EnJeu),  cleanup_jeu) // hook de sortie
    .add_systems(Update,
        jouer.run_if(in_state(AppState::EnJeu))
    )
```

---

## À retenir absolument

1. **Parallélisme automatique** : Bevy parallélise les Systems qui n'écrivent pas les mêmes Components. `.chain()` force l'ordre séquentiel.
2. **Archetype** : bloc mémoire contigu pour toutes les Entities avec la même combinaison de Components. Ajouter un Component = migration coûteuse.
3. **Relations** : exprimées via des Components spéciaux (`Parent`). Pas de hiérarchie native dans l'ECS pur.
4. **Assets** : chargement asynchrone via `AssetServer`. Le Handle est immédiat, le contenu arrive après.
5. **Changed<T>** : ne traiter que les Entities dont T a changé cette frame.
6. **States** : `OnEnter` / `OnExit` pour les transitions. `run_if` pour conditionner un System.

---

## Quiz — Questions clés

- Deux Systems écrivent des Components différents mais lisent le même — peuvent-ils être parallélisés ?
- Qu'est-ce qu'un Archetype et pourquoi la migration est-elle coûteuse ?
- Comment Bevy exprime-t-il une relation parent/enfant ?
- Pourquoi `asset_server.load()` retourne-t-il immédiatement ?
- Quelle différence entre `Changed<T>` et `With<T>` dans une Query ?

---

*Synthèse finale : comparaison de toutes les architectures, choix argumenté pour HumbleEngine.*
