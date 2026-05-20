# Unity — Fiche de révision

> Axe 1 — Architectures de moteurs de jeu
> Dernière mise à jour : Tour 2 (Intermédiaire) ✅

---

## Ce qu'on a vu au Tour 1 (rappel)

| Concept | Définition courte |
|---------|------------------|
| **GameObject** | Conteneur vide. Rien sans ses Components. |
| **Component** | Brique de comportement attachée à un GameObject. |
| **Transform** | Toujours présent. Position, rotation, échelle. |
| **`GetComponent<T>()`** | Façon standard de récupérer un Component. |

---

## 1. MonoBehaviour et le cycle de vie

Tout script attaché à un GameObject hérite de **`MonoBehaviour`**. C'est lui qui donne accès aux méthodes du cycle de vie.

```
GameObject activé
        ↓
    Awake()       ← dès l'activation, avant que les autres objets soient prêts
        ↓
    Start()       ← après tous les Awake(), juste avant la première frame
        ↓
    [chaque frame]
    Update()      ← chaque frame
    FixedUpdate() ← chaque pas physique fixe
        ↓
    OnDestroy()   ← quand le GameObject est détruit
```

| Méthode | Quand | Pour quoi |
|--------|-------|-----------|
| `Awake()` | Dès l'activation | Initialisation interne du Component lui-même |
| `Start()` | Après tous les Awake() | Initialisation qui dépend d'autres objets |
| `Update()` | Chaque frame | Logique, input, animations |
| `FixedUpdate()` | Pas physique fixe | Physique, forces, Rigidbody |

### Règle pratique : Awake pour soi, Start pour les autres.

```csharp
public class Joueur : MonoBehaviour
{
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>(); // init interne
    }

    void Start()
    {
        // tous les Awake() ont tourné — on peut dépendre des autres
        GameManager.Instance.Enregistrer(this);
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        rb.AddForce(Vector3.right * h * 5f);
    }
}
```

---

## 2. Les Prefabs

Un **Prefab** est un GameObject sauvegardé comme asset réutilisable.

```
Asset "Ennemi" (Prefab)
├── MeshRenderer
├── Rigidbody
├── Collider
└── EnemyAI (script)
```

Modifier le Prefab dans l'éditeur → toutes les instances sont mises à jour.

```csharp
[SerializeField] private GameObject enemyPrefab;

void SpawnEnemy(Vector3 position)
{
    Instantiate(enemyPrefab, position, Quaternion.identity);
}
```

### Prefab vs Scene Godot

```
Godot                Unity
──────               ──────
Scene instanciable   Prefab
Instance             Instance de Prefab
```

---

## 3. Communication avancée

### UnityEvents — découplage visuel

Un **UnityEvent** est un event configurable dans l'éditeur Unity sans toucher au code.

```csharp
public class Bouton : MonoBehaviour
{
    public UnityEvent OnClick;

    void OnMouseDown()
    {
        OnClick.Invoke();
    }
}
```

Dans l'éditeur, on branche `OnClick` vers n'importe quel Component visuellement.
Comparable aux Signaux de Godot, mais configuré dans l'inspecteur.

### ScriptableObjects — données partagées

Un **ScriptableObject** est un asset qui stocke des données indépendamment de toute scène ou GameObject.

```csharp
[CreateAssetMenu]
public class StatistiquesHeros : ScriptableObject
{
    public float vitesse = 5f;
    public int pointsDeVie = 100;
}
```

```
StatistiquesHeros.asset
        ↑           ↑
   Joueur A      Joueur B
(même asset, données partagées)
```

Idéal pour : configurations de personnages, paramètres d'armes, données de jeu partagées.

---

## 4. Différences 2D / 3D

Unity supporte les deux dans le même moteur — seuls les Components changent.

| Aspect | 3D | 2D |
|--------|----|----|
| Rendu | `MeshRenderer` | `SpriteRenderer` |
| Physique | `Rigidbody` | `Rigidbody2D` |
| Collision | `Collider` | `Collider2D` |
| Caméra | Perspective ou Orthographique | Orthographique |

---

## À retenir absolument

1. `Awake()` → init interne. `Start()` → init qui dépend des autres (après tous les Awake).
2. `Update()` → chaque frame. `FixedUpdate()` → pas physique fixe.
3. **Prefab** = GameObject réutilisable (≈ Scene instanciable Godot). Modifier le Prefab = modifier toutes les instances.
4. **UnityEvent** = event visuel découplé (≈ Signal Godot).
5. **ScriptableObject** = asset de données partagées entre GameObjects.
6. 2D/3D : même logique, Components suffixés `2D`.

---

## Quiz — Questions clés

- Quelle différence entre `Awake()` et `Start()` ?
- Qu'est-ce qu'un Prefab ? Que se passe-t-il si on le modifie dans l'éditeur ?
- À quoi sert un ScriptableObject ?
- Quel Component pour la physique d'un personnage 2D ?
- Quel est l'équivalent d'un UnityEvent dans Godot ?

---

*Tour 3 (technique) : architecture données-driven, Asset Bundles, Job System, Burst Compiler, DOTS, gestion mémoire.*
