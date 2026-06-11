# Fiche de révision — Phase 1 : Fondations système
 
> Progression : ██████████ 3/3 concepts maîtrisés ✓
 
---
 
## ✅ Mémoire : stack, heap, GC
*3 passages effectués — maîtrisé*
 
### Concepts clés
 
**Stack frame** — Bloc mémoire créé à chaque appel de méthode. Contient les variables locales et l'adresse de retour. Libéré automatiquement à la fin de la méthode.
 
**Stack vs Heap** — Les types valeur (`struct`, `int`, `bool`) vivent sur la stack. Les types référence (`class`) : la variable (pointeur) est sur la stack, l'objet lui-même est sur le heap.
 
**Boxing** — Convertir une `struct` en `object` génère une copie silencieuse vers le heap → pression GC. À éviter dans les chemins critiques.
 
**Générations GC** — Gen 0 (fréquent, rapide) → Gen 1 (tampon) → Gen 2 (rare, coûteux). Objectif en code bas-niveau : zéro allocation heap dans les chemins critiques.
 
**`stackalloc`** — Alloue sur la stack manuellement. Zéro GC, libération automatique. Limité à quelques Mo.
 
**`Span<T>`** — Vue sur une tranche mémoire contiguë (stack, heap ou natif) sans copie. Ne peut pas être champ d'une `class` (ne peut pas vivre sur le heap).
 
### Pièges
- Une `struct` dans une `class` vit sur le heap (embarquée dans l'objet).
- `Span<T>` ≠ `Memory<T>` : `Memory<T>` peut vivre sur le heap.
---
 
## ✅ OS : processus, threads, syscalls
*3 passages effectués — maîtrisé*
 
### Concepts clés
 
**Espace d'adressage virtuel** — Chaque processus a ses propres adresses virtuelles, traduites en adresses physiques par la MMU. Deux processus peuvent avoir la même adresse virtuelle sans conflit → isolation garantie.
 
**Scheduler & context switch** — Le scheduler OS alloue des tranches de temps (time slice ~1-15ms) à chaque thread. Un context switch sauvegarde l'état complet du thread (registres, pointeur de stack). Coûteux → trop de threads = trop de switches.
 
**Race condition** — `compteur++` est 3 opérations (lire, incrémenter, écrire). Deux threads simultanés peuvent produire un résultat incorrect.
 
**Visibilité inter-cœurs** — Chaque cœur a son cache. Un thread peut ne pas voir les modifications d'un autre si la variable n'est pas marquée `volatile`. `volatile` force la lecture/écriture en mémoire principale et insère des barrières mémoire (MFENCE/LFENCE).
 
**`Thread` vs `Task.Run()`** — `new Thread()` crée un vrai thread OS (syscall). `Task.Run()` réutilise un thread du ThreadPool → moins de syscalls, moins de context switches.
 
**`Interlocked`** — Utilise des instructions CPU atomiques (ex. `LOCK XADD`). Plus léger qu'un `lock` car ne bloque pas de thread.
 
### Tableau des abstractions
 
| Besoin | Outil C# | Syscall sous-jacent |
|---|---|---|
| Nouveau thread | `new Thread()` | `clone()` / `CreateThread()` |
| Tâche parallèle | `Task.Run()` | ThreadPool (threads recyclés) |
| Opération atomique | `Interlocked` | Instruction CPU atomique |
| Exclusion mutuelle | `lock` | `futex()` / `CriticalSection` |
| Lancer un programme | `Process.Start()` | `fork()`+`exec()` / `CreateProcess()` |
 
### Tableau visibilité vs atomicité
 
| Problème | Outil |
|---|---|
| Visibilité inter-cœurs | `volatile` |
| Atomicité d'une opération | `Interlocked` |
| Section critique complexe | `lock` |
 
### Pièges
- `volatile` ne règle pas les race conditions — il garantit la visibilité, pas l'atomicité.
- `volatile int compteur; compteur++` est toujours une race condition.
---
 
## ✅ Interop C# ↔ natif : P/Invoke, unsafe, Span\<T\>
*3 passages effectués — maîtrisé*
 
### Concepts clés
 
**P/Invoke** — Mécanisme pour appeler des fonctions dans des DLL natives (.dll / .so). Le runtime charge la DLL, localise la fonction, marshalle les arguments, bascule managé → natif, exécute, puis marshalle le retour.
 
**Marshalling** — Conversion des types C# en représentation C. Gratuit pour les types blittables (même représentation binaire des deux côtés : `int`, `float`, `struct` de blittables). Coûteux pour les non-blittables (`bool`, `string`, `char`).
 
**`unsafe` + `fixed`** — `unsafe` autorise les pointeurs directs. `fixed` épingle un objet heap pour que le GC ne le déplace pas pendant un appel natif. `stackalloc` évite le besoin de `fixed` (la stack ne bouge pas).
 
**`[StructLayout(LayoutKind.Sequential)]`** — Garantit que .NET ne réordonne pas les champs d'une struct pour optimisation. Obligatoire pour les structs passées à du code natif.
 
**`IntPtr`** — Type C# pour un pointeur opaque. S'adapte à la plateforme (4 bytes en 32-bit, 8 bytes en 64-bit). Pas de `LongPtr` — `IntPtr` couvre les deux cas.
 
**`SafeHandle`** — Wrapper pour ressources natives longévives. Garantit la libération via le finalizer GC, même sans `finally` explicite.
 
**`UnmanagedCallersOnly`** — Expose une méthode C# comme callback appelable depuis du code natif. Attention : sur un thread natif hors supervision du runtime → pas d'allocations, pas d'exceptions managées.
 
### Tableau : choisir son outil
 
| Situation | Outil |
|---|---|
| Appeler une fonction native | P/Invoke (`DllImport`) |
| Passer un buffer sans copie | `Span<T>` + `stackalloc` |
| Pointeurs et arithmétique mémoire | `unsafe` + `fixed` |
| Wrapper une ressource native longévive | `SafeHandle` |
| Callback natif → C# | `UnmanagedCallersOnly` |
 
### Pièges
- `bool` non-blittable : marshallé en `int` (4 bytes). Coûteux si appelé en boucle.
- `finally` seul ne suffit pas pour les ressources natives — préférer `SafeHandle`.
- Dans un callback `UnmanagedCallersOnly` : pas d'allocations heap, pas d'appels managés.
---
 
*Dernière mise à jour : Phase 1 complète ✓*