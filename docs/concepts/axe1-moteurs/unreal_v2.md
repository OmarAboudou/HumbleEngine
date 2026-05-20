# Unreal Engine — Fiche de révision

> Axe 1 — Architectures de moteurs de jeu
> Dernière mise à jour : Tour 2 (Intermédiaire) ✅

---

## Ce qu'on a vu au Tour 1 (rappel)

| Concept | Définition courte |
|---------|------------------|
| **Actor** | Tout objet placé dans le Level. A une existence propre. |
| **Component** | Brique de comportement attachée à un Actor. |
| **Pawn / Character** | Actor contrôlable (par un joueur ou une IA). |
| **Controller** | Sépare la logique de contrôle de la représentation physique. |
| **Level** | L'espace de jeu (≈ Scène dans Godot/Unity). |

---

## 1. Cycle de vie des Actors

```
Actor créé / spawné
        ↓
    Constructor()     ← initialisation des valeurs par défaut (aussi dans l'éditeur)
        ↓
    BeginPlay()       ← une fois, quand l'Actor entre dans la simulation
        ↓
    [chaque frame]
    Tick(DeltaTime)   ← chaque frame (équivalent de Update() Unity)
        ↓
    EndPlay()         ← quand l'Actor est retiré du jeu
        ↓
    Destroyed()       ← juste avant la destruction
```

| Méthode | Quand | Pour quoi |
|--------|-------|-----------|
| `Constructor` | À la création (éditeur inclus) | Valeurs par défaut des Components |
| `BeginPlay()` | Une fois, au démarrage de la simulation | Initialisation runtime |
| `Tick(DeltaTime)` | Chaque frame | Logique continue |
| `EndPlay()` | Quand l'Actor quitte le jeu | Nettoyage, sauvegarde |

### Le DeltaTime

Même principe que `delta` dans Godot — temps écoulé depuis la dernière frame.

```cpp
void AJoueur::Tick(float DeltaTime)
{
    Super::Tick(DeltaTime);
    AddMovementInput(GetActorForwardVector(), vitesse * DeltaTime);
}
```

---

## 2. Blueprint vs C++

| Critère | Blueprint | C++ |
|--------|-----------|-----|
| **Format** | Graphe visuel de nœuds | Code C++ |
| **Accessibilité** | Designers, artistes | Développeurs |
| **Performance** | Légèrement plus lent | Optimal |
| **Itération** | Rapide, pas de compilation | Compilation requise |
| **Cas d'usage** | Prototypage, logique de gameplay | Systèmes core, performances critiques |

### Le workflow hybride recommandé

```
C++                        Blueprint
────────────────           ────────────────
Systèmes de base           Logique de gameplay
Performances critiques     Comportements d'ennemis
Architecture du moteur     UI, cinématiques, tweaks designers
```

On écrit les fondations en C++, on expose des propriétés aux Blueprints.

```cpp
// C++ — propriété exposée au Blueprint
UPROPERTY(EditAnywhere, BlueprintReadWrite)
float Vitesse = 600.0f;
```

---

## 3. Le système de possession en détail

```cpp
// Le Controller prend le contrôle d'un Pawn
PlayerController->Possess(MonPawn);

// Le Controller lâche le contrôle
PlayerController->UnPossess();
```

### Pourquoi c'est puissant

```
Scénario : le joueur meurt
→ PlayerController::UnPossess()
→ PlayerController::Possess(SpectatorPawn)
→ Mode spectateur sans changer de Controller

Scénario : IA prend le relais
→ AIController::Possess(Character)
→ Même Character, logique différente — zéro duplication
```

---

## 4. Le Gameplay Framework

Ensemble de classes conçues pour travailler ensemble.

| Classe | Rôle |
|--------|------|
| `AGameMode` | Les **règles** du jeu — conditions de victoire, spawn. Serveur uniquement. |
| `AGameState` | L'**état global** partagé avec tous les clients (score, temps…) |
| `APlayerState` | L'état d'un joueur spécifique (nom, score perso) |
| `APlayerController` | Gère les inputs, possède un Pawn |
| `APawn / ACharacter` | Représentation physique dans le monde |
| `AHUD` | UI affichée pour le joueur |

```
AGameMode          ← règles (serveur seulement)
AGameState         ← état global (tous les clients)
    └── APlayerState × N  ← état par joueur
APlayerController  ← inputs (un par joueur)
    └── ACharacter         ← représentation physique
        └── AHUD           ← UI du joueur
```

⚠️ **GameMode ≠ GameState** : GameMode = les règles (qui gagne, comment). GameState = l'état partagé (le score actuel, le temps restant).

---

## 5. Communication via Delegates et Event Dispatchers

### Delegate (C++)

```cpp
// Déclaration
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnSanteChangee, float, NouvelleValeur);

// Dans la classe
UPROPERTY(BlueprintAssignable)
FOnSanteChangee OnSanteChangee;

// Émission
OnSanteChangee.Broadcast(80.0f);

// Abonnement
Joueur->OnSanteChangee.AddDynamic(this, &AMaClasse::SurSanteChangee);
```

### Comparaison avec Godot et Unity

```
Godot          Unity               Unreal
──────         ──────              ──────
Signal         UnityEvent          Delegate / Event Dispatcher
emit_signal()  OnClick.Invoke()    OnSanteChangee.Broadcast()
connect()      (inspecteur)        AddDynamic()
```

---

## À retenir absolument

1. `Constructor` = valeurs par défaut (éditeur inclus). `BeginPlay()` = initialisation runtime.
2. Workflow hybride : C++ pour les systèmes core, Blueprint pour les designers.
3. `Possess()` / `UnPossess()` permettent de swapper dynamiquement la logique de contrôle.
4. `GameMode` = règles (serveur). `GameState` = état partagé (tous les clients).
5. Delegate / Event Dispatcher = l'équivalent des Signaux Godot / UnityEvents.

---

## Quiz — Questions clés

- Quelle différence entre `BeginPlay()` et le Constructor ?
- Dans quel cas choisis-tu Blueprint plutôt que C++ ?
- Que se passe-t-il quand un PlayerController appelle `Possess()` ?
- Quelle différence entre `GameMode` et `GameState` ?
- Quel est l'équivalent d'un Delegate Unreal dans Godot ? Dans Unity ?

---

*Tour 3 (technique) : cycle de réplication réseau, Subsystems, Enhanced Input, Chaos Physics, optimisations (LOD, occlusion culling), packaging.*
