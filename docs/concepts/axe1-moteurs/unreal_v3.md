# Unreal Engine — Fiche de révision

> Axe 1 — Architectures de moteurs de jeu
> Dernière mise à jour : Tour 3 (Technique) ✅

---

## Ce qu'on a vu au Tour 2 (rappel)

| Concept | Définition courte |
|---------|------------------|
| `Constructor` | Valeurs par défaut (éditeur inclus). |
| `BeginPlay()` | Une fois, au démarrage de la simulation. |
| `Tick(DeltaTime)` | Chaque frame. |
| Blueprint vs C++ | Designers vs systèmes core. Workflow hybride recommandé. |
| `Possess()` / `UnPossess()` | Swap dynamique de la logique de contrôle. |
| `GameMode` | Règles du jeu — serveur uniquement. |
| `GameState` | État global partagé avec tous les clients. |
| Delegate / Event Dispatcher | Équivalent des Signaux Godot / UnityEvents. |

---

## 1. Réplication réseau

### L'idée centrale

Unreal est conçu dès le départ pour le multijoueur. La réplication synchronise l'état du serveur vers les clients.

```
Serveur  →  état autoritaire (la vérité)
Clients  →  reçoivent les mises à jour du serveur
```

### Répliquer une variable

```cpp
UPROPERTY(Replicated)
int32 Score;

void AMonActor::GetLifetimeReplicatedProps(
    TArray<FLifetimeProperty>& OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    DOREPLIFETIME(AMonActor, Score);
}
```

### ReplicatedUsing — réagir à une mise à jour

```cpp
UPROPERTY(ReplicatedUsing = OnRep_Score)
int32 Score;

UFUNCTION()
void OnRep_Score()
{
    // Appelé automatiquement sur le client quand Score change
    MettreAJourAffichage(Score);
}
```

### RPCs — appels de fonctions à distance

| Type | Direction | Usage |
|------|-----------|-------|
| `Server` | Client → Serveur | "Je veux faire une action" |
| `Client` | Serveur → Client spécifique | "Affiche cet effet pour toi" |
| `NetMulticast` | Serveur → Tous les clients | "Joue cette explosion pour tout le monde" |

```cpp
UFUNCTION(Server, Reliable)
void ServerTirer();  // le client demande au serveur de valider le tir
```

---

## 2. Subsystems

### Le problème des Singletons classiques

Un Singleton global = cycle de vie mal contrôlé, difficile à tester.

### La solution — Subsystems

Singletons gérés automatiquement par Unreal, avec un cycle de vie lié à un objet Unreal existant.

| Type | Cycle de vie lié à | Cas d'usage |
|------|-------------------|-------------|
| `UGameInstanceSubsystem` | La GameInstance (toute la session) | Sauvegarde, analytics |
| `UWorldSubsystem` | Le World (une partie) | Gestion des spawns |
| `ULocalPlayerSubsystem` | Un joueur local | Input, UI par joueur |

```cpp
// Déclaration
UCLASS()
class UScoreSubsystem : public UGameInstanceSubsystem
{
    GENERATED_BODY()
public:
    void AjouterPoints(int32 Points);
    int32 GetScore() const { return Score; }
private:
    int32 Score = 0;
};

// Accès depuis n'importe où
UScoreSubsystem* Scores = GetGameInstance()
    ->GetSubsystem<UScoreSubsystem>();
Scores->AjouterPoints(10);
```

Unreal crée et détruit le Subsystem automatiquement avec son objet parent.

---

## 3. Enhanced Input

### Deux concepts clés

**Input Action** — une intention abstraite, indépendante de la touche physique.
```
IA_Sauter    ← "l'intention de sauter", peu importe la touche
IA_Tirer     ← "l'intention de tirer"
```

**Input Mapping Context** — un ensemble de mappings touche → action, activable/désactivable au runtime.
```
IMC_APied    ← Espace → IA_Sauter, Clic gauche → IA_Tirer
IMC_Vehicule ← Espace → IA_Frein,  Clic gauche → IA_Klaxon
```

```cpp
// Activer un contexte
PlayerController->AddMappingContext(IMC_APied, 0);

// Switcher de contexte
PlayerController->RemoveMappingContext(IMC_APied);
PlayerController->AddMappingContext(IMC_Vehicule, 0);
```

---

## 4. Chaos Physics

Moteur physique natif d'Unreal depuis UE5. Il gère :

- Physique des corps rigides classique
- **Destruction procédurale** (Geometry Collections) — fractures précalculées, simulées au runtime
- **Vêtements et tissus** (Chaos Cloth)
- **Cheveux et fibres** (Chaos Hair)

```cpp
// Déclencher la destruction d'un objet fracturé
GeometryCollectionComponent->SetSimulatePhysics(true);
```

Les fractures sont précalculées dans l'éditeur — pas générées à la volée. C'est ce qui permet des destructions spectaculaires sans exploser les performances.

---

## 5. Optimisations

### LOD — Level of Detail

Un mesh est représenté par plusieurs versions de complexité décroissante. Unreal switche automatiquement selon la distance.

```
Distance 0-10m   →  LOD 0  (50 000 polygones)
Distance 10-50m  →  LOD 1  (15 000 polygones)
Distance 50-200m →  LOD 2  (3 000 polygones)
Distance 200m+   →  LOD 3  (500 polygones)
```

### Occlusion Culling

Ne pas rendre ce que la caméra ne peut pas voir — même si c'est dans le champ de vision.

```
Caméra dans un couloir
→ la pièce derrière le mur n'est pas rendue
→ même si elle est théoriquement "devant" la caméra
```

### Nanite — UE5

Système de géométrie virtualisée qui rend obsolète la gestion manuelle des LODs pour les meshes statiques.

```
Sans Nanite : LOD0, LOD1, LOD2 créés manuellement
Avec Nanite : Unreal streame automatiquement le niveau de détail
              exact nécessaire pour chaque pixel à l'écran
```

---

## 6. Packaging

### Le pipeline

```
Projet Unreal
    ↓  Package
├── Windows  →  MonJeu.exe + dossiers de contenu
├── Linux    →  binaire Linux
├── Android  →  .apk
└── Console  →  selon les accords de développeur
```

### Cook — préparation des assets

Unreal convertit chaque asset dans le format optimal pour la plateforme cible.

```
Texture PNG  →  DXT/BC (Windows) / ASTC (Android) / ETC2 (iOS)
Blueprint    →  bytecode natif
Shader       →  compilé pour le GPU cible
```

### Shipping vs Development

| Build | Usage | Caractéristiques |
|-------|-------|-----------------|
| `Development` | Tests internes | Logs, console de debug, assertions actives |
| `Shipping` | Distribution finale | Tout retiré — plus léger, plus rapide |

---

## À retenir absolument

1. **Réplication** : `Replicated` synchronise la valeur. `ReplicatedUsing` exécute une fonction en plus.
2. **RPCs** : Server (client→serveur) / Client (serveur→un client) / NetMulticast (serveur→tous).
3. **Subsystems** : Singletons à cycle de vie contrôlé, liés à GameInstance, World ou LocalPlayer.
4. **Enhanced Input** : Input Action = intention abstraite. Input Mapping Context = mapping touche → action, swappable au runtime.
5. **LOD** : plusieurs versions d'un mesh selon la distance. Nanite automatise ça en UE5.
6. **Occlusion Culling** : ne pas rendre ce qui est caché derrière d'autres objets.
7. **Shipping** : build final allégé — logs et outils de debug retirés.

---

## Quiz — Questions clés

- Quelle différence entre `Replicated` et `ReplicatedUsing` ?
- Quel type de RPC utilises-tu pour jouer une explosion visible par tous les clients ?
- Pourquoi les Subsystems sont-ils préférables aux Singletons classiques ?
- Un joueur entre dans une voiture — comment tu gères le changement de contrôles avec Enhanced Input ?
- Qu'est-ce que Nanite change par rapport à la gestion manuelle des LODs ?

---

*Synthèse finale : comparaison de toutes les architectures, choix argumenté pour HumbleEngine.*
