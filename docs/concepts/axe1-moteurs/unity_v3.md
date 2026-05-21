# Unity — Fiche de révision

> Axe 1 — Architectures de moteurs de jeu
> Dernière mise à jour : Tour 3 (Technique) ✅

---

## Ce qu'on a vu au Tour 2 (rappel)

| Concept | Définition courte |
|---------|------------------|
| `Awake()` | Init interne du Component. Avant tous les Start(). |
| `Start()` | Init qui dépend d'autres objets. Après tous les Awake(). |
| `Update()` | Chaque frame. `FixedUpdate()` = pas physique fixe. |
| **Prefab** | GameObject réutilisable (≈ Scene instanciable Godot). |
| **UnityEvent** | Event visuel découplé (≈ Signal Godot). |
| **ScriptableObject** | Asset de données partagées entre GameObjects. |

---

## 1. Architecture data-driven

### Le problème

Données codées en dur dans les MonoBehaviours = 15 types d'ennemis → 15 classes → modifier l'équilibre du jeu nécessite de toucher au code.

### La solution — séparer données et comportement

```csharp
// Les données — un ScriptableObject par type
[CreateAssetMenu]
public class EnnemiData : ScriptableObject
{
    public float vitesse;
    public int degats;
    public float porteeDetection;
}

// Le comportement — un seul MonoBehaviour pour tous les types
public class Ennemi : MonoBehaviour
{
    [SerializeField] private EnnemiData data;

    void Update()
    {
        transform.Translate(data.vitesse * Time.deltaTime);
    }
}
```

```
Assets/
├── Ennemis/
│   ├── GoblinData.asset      ← vitesse: 3, dégats: 5
│   ├── TrollData.asset       ← vitesse: 1, dégats: 20
│   └── ArcherData.asset      ← vitesse: 4, dégats: 8
└── Scripts/
    └── Ennemi.cs             ← un seul script pour les trois
```

Modifier l'équilibre = modifier un asset, pas du code. Accessible aux designers sans toucher à C#.

---

## 2. Asset Bundles et Addressables

### Le problème

Embarquer tous les assets dans le build = binaire lourd, tout chargé en mémoire dès le démarrage.

### Asset Bundles

Paquets d'assets chargés et déchargés dynamiquement à l'exécution.

```csharp
AssetBundle bundle = AssetBundle.LoadFromFile("chemin/vers/bundle");
GameObject prefab = bundle.LoadAsset<GameObject>("Troll");
Instantiate(prefab);
bundle.Unload(true);  // libère la mémoire
```

### Addressables — l'évolution moderne

Système de haut niveau au-dessus des Asset Bundles. On identifie les assets par une **adresse** (string), pas par un chemin physique. Unity gère le téléchargement, le cache et le chargement en arrière-plan.

```csharp
var handle = Addressables.LoadAssetAsync<GameObject>("Ennemis/Troll");
await handle.Task;
Instantiate(handle.Result);
Addressables.Release(handle);  // libération explicite
```

Base du DLC et du streaming de contenu.

---

## 3. Job System et Burst Compiler

### Le Job System

Unités de travail distribuées automatiquement sur tous les cœurs CPU — sans gérer les threads manuellement.

```csharp
[BurstCompile]
struct DeplacementJob : IJobParallelFor
{
    public NativeArray<float3> positions;
    public NativeArray<float3> vitesses;
    public float deltaTime;

    public void Execute(int index)
    {
        positions[index] += vitesses[index] * deltaTime;
    }
}

JobHandle handle = job.Schedule(positions.Length, 64);
handle.Complete();
```

### Le Burst Compiler

Recompile le code des Jobs en **instructions CPU natives optimisées** (vectorisation SIMD, cache-friendly, sans GC).

```
Job sans Burst  →  ~8ms
Job avec Burst  →  ~0.3ms   (×25 plus rapide)
```

S'active avec `[BurstCompile]` sur la struct du Job.

---

## 4. DOTS

Réécriture complète de l'architecture Unity vers un **ECS natif** combiné avec le Job System et Burst.

```
Unity classique          Unity DOTS
───────────────          ──────────
GameObject               Entity
MonoBehaviour            Component (struct pure)
Script Update()          System
Mémoire fragmentée       Mémoire contiguë (chunks)
Single-threaded          Multi-threaded par défaut
```

### Pourquoi la mémoire contiguë change tout

```
Unity classique — GameObjects éparpillés :
[GO1]....[GO2]......[GO3]..[GO4]   ← cache miss fréquents

DOTS — Entities contiguës par archetype :
[E1][E2][E3][E4][E5][E6]...        ← lecture séquentielle, cache friendly
```

Stable depuis Unity 2022. Indispensable pour les simulations massives (milliers d'entités), courbe d'apprentissage importante.

---

## 5. Gestion mémoire

### Le Garbage Collector

Le GC libère automatiquement la mémoire inutilisée — mais **pause le thread principal** quand il se déclenche.

```csharp
// Mauvais — allocation temporaire à chaque frame → pression sur le GC
void Update()
{
    var ennemis = new List<Ennemi>();
}

// Bon — allocation unique, réutilisation
private List<Ennemi> ennemis = new List<Ennemi>();

void Update()
{
    ennemis.Clear();  // pas d'allocation
}
```

### NativeArray — mémoire hors GC

Utilisé dans les Jobs. Alloué en mémoire native, le GC ne le touche jamais. Libération manuelle obligatoire.

```csharp
var positions = new NativeArray<float3>(10000, Allocator.TempJob);
// ... utilisation dans un Job
positions.Dispose();  // obligatoire
```

---

## À retenir absolument

1. **Data-driven** : données dans des ScriptableObjects, comportement dans les MonoBehaviours. Un script pour N variantes.
2. **Addressables** : chargement/déchargement dynamique d'assets par adresse. Base du streaming et du DLC.
3. **Job System** : distribution automatique sur tous les cœurs CPU. Pas de gestion manuelle de threads.
4. **Burst** : `[BurstCompile]` → instructions CPU natives. Gains x10 à x25 sur les Jobs.
5. **DOTS** : ECS natif + Job System + Burst. Mémoire contiguë = cache friendly = performances massives.
6. **GC** : éviter les allocations temporaires dans `Update()`. Préférer `NativeArray` dans les Jobs.

---

## Quiz — Questions clés

- Pourquoi séparer les données d'un ennemi dans un ScriptableObject plutôt que dans son MonoBehaviour ?
- Quelle différence entre Asset Bundles et Addressables ?
- Qu'est-ce que le Job System apporte par rapport aux threads manuels ?
- Pourquoi le Burst Compiler est-il bien plus rapide que du C# classique ?
- Quel est le problème des allocations dans `Update()` par rapport au GC ?

---

*Synthèse finale : comparaison de toutes les architectures, choix argumenté pour HumbleEngine.*
