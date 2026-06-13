# Fiche de révision — Phase 4 : Architecture plugin
 
> Progression : ██████████ 4/4 blocs maîtrisés ✓
 
---
 
## ✅ Bloc 1 — Chargement dynamique
*2 passages effectués — maîtrisé*
 
### Concepts clés
 
**`Assembly.LoadFrom`** — Charge un assembly dans le contexte default. L'assembly reste en mémoire jusqu'à la fin du processus, même si toutes les références sont perdues. Pas de déchargement possible.
 
**`AssemblyLoadContext` (ALC)** — Bulle d'isolation. Le runtime identifie un type par le triplet `(ALC, AssemblyName, TypeName)`. Deux types de même nom dans deux ALC différents sont des types distincts — cast impossible entre eux.
 
**Pattern interface partagée** — Les interfaces sont chargées dans le default ALC (partagé). Les plugins implémentent ces interfaces dans leur propre ALC. Le programme principal caste vers l'interface, jamais vers le type concret du plugin.
 
**`isCollectible: true`** — Requis pour pouvoir décharger un ALC. Le déchargement est déclenché par `Unload()` mais effectif seulement quand le GC collecte **et** qu'aucune référence ne pointe vers l'ALC.
 
**Blocages de déchargement courants :**
- Thread encore en cours dans le plugin (frames sur la stack)
- Delegate capturant un objet du plugin
- Objet `Type` gardé en cache
- Event handler non désinscrit
**`WeakReference` pour vérifier le déchargement :**
```csharp
var weakRef = new WeakReference(alc);
alc.Unload();
alc = null;
for (int i = 0; i < 10 && weakRef.IsAlive; i++)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
}
```
 
**Résolution des dépendances** — Surcharger `Load()` dans l'ALC pour contrôler d'où viennent les dépendances. Retourner `null` = déléguer au default ALC (assemblies partagés).
 
---
 
## ✅ Bloc 2 — Interfaces & contrats, versioning, ABI
*2 passages effectués — maîtrisé*
 
### Concepts clés
 
**ABI vs API**
- **API** : contrat au niveau du code source (noms, signatures)
- **ABI** : contrat au niveau binaire (disposition mémoire, calling convention)
**Changements breaking vs non-breaking :**
 
| Breaking | Non-breaking |
|---|---|
| Ajouter une méthode à une interface | Ajouter une nouvelle interface |
| Renommer / supprimer une méthode | Ajouter un paramètre optionnel |
| Changer le type d'un paramètre | Ajouter un nouveau type |
| Changer l'ordre des champs d'une struct | Ajouter une méthode à une classe |
 
**Metadata tokens** — Le compilateur stocke des indices dans les tables de métadonnées, pas des strings. Si la signature cible a changé, la résolution échoue avec `MissingMethodException`.
 
**Stratégie de versioning** — Ne jamais modifier une interface existante. Créer `IPlugin2 : IPlugin` pour les nouvelles capacités. Détecter les capacités à runtime avec `is` :
```csharp
if (plugin is IPlugin2 p2) p2.OnUpdate(dt);
```
 
**Default Interface Methods (C# 8+)** — Permet d'ajouter des méthodes à une interface avec une implémentation par défaut. Les implémenteurs existants n'ont pas à les définir. Accessibles uniquement via l'interface, pas via le type concret.
 
**`AssemblyVersion`** — Version ABI utilisée par le runtime pour résoudre les références. La changer est breaking.
**`AssemblyInformationalVersion`** — Version lisible humain (SemVer, git hash). Ignorée par le runtime.
 
**`[StructLayout(LayoutKind.Sequential)]`** — Obligatoire pour les structs traversant la frontière plugin. `LayoutKind.Explicit` + `[FieldOffset]` pour contrôle total des offsets.
 
---
 
## ✅ Bloc 3 — Patterns : registry, event bus, hot-reload
*2 passages effectués — maîtrisé*
 
### Plugin Registry
 
Dictionnaire central mappant un type d'interface vers son implémentation. Le programme principal ne référence jamais directement les types concrets des plugins.
 
```csharp
void Register<T>(T plugin) where T : IPlugin => _plugins[typeof(T)] = plugin;
T Get<T>() where T : IPlugin => (T)_plugins[typeof(T)];
```
 
**Découverte automatique** — Scanner un répertoire avec `IsAssignableFrom`. Attention : `typeof(IPlugin)` doit venir du même ALC (default) que lors du chargement du plugin, sinon `IsAssignableFrom` retourne `false`.
 
### Event Bus
 
Dictionnaire `Type → List<Delegate>`. Les plugins communiquent sans se connaître directement.
 
**Fuite mémoire** — Un handler non désinscrit maintient une référence vers le subscriber → bloque le déchargement de l'ALC. Solution : `Unsubscribe` explicite dans `Shutdown()`, ou `WeakReference` sur les handlers.
 
**Piège WeakReference + lambda** — Une lambda anonyme non référencée ailleurs est collectée immédiatement. Les handlers doivent être des champs d'instance.
 
### Hot-Reload
 
Séquence :
1. `FileSystemWatcher` détecte le changement (debounce nécessaire)
2. `Shutdown()` + désinscription event bus
3. `Unload()` + attendre déchargement effectif
4. Charger le nouveau fichier dans un nouvel ALC
5. `Initialize()` + ré-enregistrer dans le registry
**Fichier verrouillé (Windows)** — Un `.dll` chargé est verrouillé par l'OS. Il faut que l'ancien ALC soit déchargé avant de pouvoir écraser le fichier. Solutions : shadow copying (copie dans un répertoire temporaire) ou chargement depuis `MemoryStream`. Sur Linux : pas de verrou (inodes).
 
---
 
## ✅ Bloc 4 — Sécurité : sandboxing, permissions
*2 passages effectués — maîtrisé*
 
### Concepts clés
 
**CAS mort** — Code Access Security supprimé dans .NET Core. Contournable via `unsafe`, P/Invoke, réflexion. Fausse sécurité.
 
**Isolation par processus** — La seule vraie garantie. Le plugin tourne dans un processus enfant, l'OS garantit l'isolation mémoire. Communication via IPC (named pipes, sockets, shared memory).
 
**Canaux IPC :**
 
| Mécanisme | Usage |
|---|---|
| `NamedPipeServerStream` | Cas général, bidirectionnel |
| `Socket` localhost | Architecture distribuée |
| `MemoryMappedFile` | Données volumineuses, temps-réel |
 
**Injection de dépendances contrôlée** — Le plugin reçoit uniquement des services filtrés via interface (`IFileSystem`, `ILogger`...). Jamais de référence directe au programme principal.
 
**`RestrictedFileSystem`** — Toujours résoudre avec `Path.GetFullPath` avant de vérifier le préfixe autorisé. Sans ça, `../` contourne le contrôle (path traversal).
 
**`LoadUnmanagedDll`** — Intercepte les résolutions de DLL natives dynamiques. Limite : n'intercepte pas les appels vers des DLL déjà chargées dans le processus.
 
**seccomp (Linux)** — Restreint les syscalls autorisés au niveau OS. `SECCOMP_MODE_STRICT` (très restrictif) ou `SECCOMP_MODE_FILTER` + BPF (liste fine). Appelable via P/Invoke (`prctl`).
 
**AppContainer (Windows)** — Niveau d'intégrité réduit, bloque réseau / registre / filesystem par défaut.
 
### Défense en profondeur
 
```
Niveau 1 — Contrat (interfaces)         → limite la surface d'appel
Niveau 2 — Injection contrôlée          → services filtrés uniquement
Niveau 3 — AssemblyLoadContext           → isolation types + blocage LoadUnmanagedDll
Niveau 4 — Processus séparé (IPC)       → isolation mémoire OS
Niveau 5 — seccomp / AppContainer       → restriction syscalls OS
```
 
Chaque niveau suppose que les niveaux inférieurs peuvent être contournés.
 
---
 
*Dernière mise à jour : Phase 4 complète ✓*