# Unity — Fiche de révision

> Axe 1 — Architectures de moteurs de jeu
> Dernière mise à jour : Tour 1 (Vulgarisation) ✅

---

## L'idée centrale

Un objet de jeu est une **coquille vide** à laquelle on attache des **comportements**. C'est ce qu'on lui colle dessus qui le définit — pas ce qu'il est intrinsèquement.

---

## Les concepts fondamentaux

| Concept | Définition courte |
|---------|------------------|
| **GameObject** | Conteneur vide. Ne fait rien seul — c'est ses Components qui le définissent. |
| **Component** | Bloc de comportement attaché à un GameObject (affichage, physique, son…). |
| **Transform** | Component obligatoire sur tout GameObject. Définit position, rotation, échelle. |
| **Scène** | L'espace qui contient tous les GameObjects actifs, organisés en hiérarchie. |

---

## Le GameObject

- Un conteneur vide sans aucune fonctionnalité intrinsèque.
- Seul, il n'affiche rien, ne bouge pas, n'interagit avec rien.
- Ce sont les **Components** qu'on lui attache qui lui donnent vie.

---

## Le Component

Exemples de Components natifs :

| Component | Ce qu'il fait |
|---|---|
| `Transform` | Position, rotation, échelle — **toujours présent** |
| `MeshRenderer` | Affiche un modèle 3D |
| `Rigidbody` | Ajoute la physique (gravité, collisions) |
| `AudioSource` | Joue des sons |
| `Camera` | Définit ce que voit le joueur |

Un personnage joueur dans Unity :

```
GameObject "Joueur"
├── Transform         ← toujours présent
├── MeshRenderer      ← affiche le modèle
├── Rigidbody         ← gère la physique
├── AudioSource       ← joue les sons
└── PlayerController  ← script de comportement custom
```

---

## La Scène

- Contient tous les GameObjects actifs.
- Les GameObjects peuvent être **parentés** pour former une hiérarchie.

```
Scène "Niveau"
├── GameObject "Terrain"
├── GameObject "Joueur"
│   └── GameObject "Caméra"   ← enfant du Joueur, suit ses mouvements
└── GameObject "Ennemi"
```

---

## Communication entre objets

Pas de Signaux comme dans Godot. Unity utilise principalement :

- **`GetComponent<T>()`** — récupère directement un Component sur un GameObject.
- **Events** — pour des communications plus découplées (moins fréquent).

Plus direct que les Signaux de Godot, mais aussi plus couplé.

---

## Comparaison rapide avec Godot

```
Godot                          Unity
──────────────────────         ──────────────────────
Scene "Joueur"                 GameObject "Joueur"
├── Sprite2D                   ├── Transform
├── CollisionShape2D           ├── MeshRenderer
└── AudioStreamPlayer          ├── Rigidbody
                               └── AudioSource
```

Même résultat, philosophie inverse :
- Godot **décompose** en objets séparés dans un arbre.
- Unity **empile** des comportements sur un seul objet.

---

## À retenir absolument

1. **GameObject** = conteneur vide. Rien sans ses Components.
2. **Transform** = toujours là, sur tout GameObject.
3. **Component** = brique de comportement. On compose par empilement.
4. **`GetComponent<T>()`** = façon standard de communiquer entre objets.

---

## Quiz — Questions clés

- Que fait un GameObject sans aucun Component ?
- Quel Component est présent sur absolument tous les GameObjects ?
- Comment ajouter la physique à un objet dans Unity ?
- Quelle est la différence fondamentale entre l'approche de Godot et celle de Unity ?
- Comment un script récupère-t-il un Component dans Unity ?

---

*Tour 2 (intermédiaire) : cycle de vie des Components (Awake, Start, Update…), MonoBehaviour, prefabs, communication avancée (UnityEvents, ScriptableObjects), différences 2D/3D.*
