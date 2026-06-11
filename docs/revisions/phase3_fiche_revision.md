# Fiche de révision — Phase 3 : Programmation graphique
 
> Progression : ██████████ 4/4 concepts maîtrisés ✓
 
---
 
## ✅ Pipeline de rendu
*3 passages effectués — maîtrisé*
 
### Vue d'ensemble
 
Le pipeline de rendu est la chaîne de transformation qui convertit des vertices 3D en pixels à l'écran. Le CPU délègue ce travail au GPU. Les données d'entrée sont des **vertices** stockés dans un **VBO** (Vertex Buffer Object).
 
```
[Vertices / VBO]
      ↓
 Vertex Shader      ← programmable (tu l'écris)
      ↓
 Rasterisation      ← fixe (câblé GPU)
 + Interpolation    ← fixe
      ↓
Fragment Shader     ← programmable (tu l'écris)
      ↓
  Depth Test        ← fixe (configurable)
      ↓
 Framebuffer → écran
```
 
---
 
### Les données d'un vertex
 
Un vertex est un paquet d'attributs. Tu en définis la structure (le **vertex layout**) :
 
| Attribut | Type | Usage |
|---|---|---|
| `position` | `(x, y, z)` | Obligatoire |
| `normale` | `(nx, ny, nz)` | Direction perpendiculaire à la surface (éclairage) |
| `UV` | `(u, v)` | Coordonnées dans une texture |
| `couleur` | `(r, g, b)` | Couleur par vertex |
 
---
 
### Vertex Shader
 
- S'exécute **une fois par vertex**, en parallèle sur tous les vertices
- Reçoit les attributs du vertex, produit une position en **clip space** (coordonnées normalisées : tout le visible entre -1 et +1)
- Applique la **MVP matrix** : combinaison de 3 matrices
| Matrice | Transformation |
|---|---|
| **Model** | Coordonnées locales → world space (translation, rotation, scale) |
| **View** | World space → espace caméra (position + orientation caméra) |
| **Projection** | Espace caméra → clip space (perspective, normalisation) |
 
---
 
### Rasterisation & Interpolation
 
- Détermine **quels pixels** sont couverts par la primitive
- Génère un **fragment** pour chaque pixel couvert
- **Interpole automatiquement** les attributs entre les vertices (ex: dégradé de couleur entre deux sommets)
- Étape **fixe** — non programmable, câblée dans le hardware
---
 
### Fragment Shader
 
- S'exécute **une fois par fragment**, en parallèle
- Reçoit les attributs **interpolés** depuis le vertex shader
- Produit une couleur `(r, g, b, a)`
- Peut accéder aux **uniforms** : valeurs envoyées depuis le CPU, **constantes pour tout le draw call** (ex: position lumière, temps)
```glsl
// Exemple GLSL simplifié
uniform vec3 lightPosition;   // constant pour tous les fragments
in vec3 fragmentNormal;       // interpolé depuis vertex shader
 
void main() {
    float brightness = dot(normalize(fragmentNormal), normalize(lightPosition));
    gl_FragColor = vec4(brightness, brightness, brightness, 1.0);
}
```
 
---
 
### Depth Buffer
 
- Image de même taille que le framebuffer, stocke la **profondeur** (0→1) de chaque pixel
- À chaque fragment : compare sa profondeur avec la valeur existante
  - Plus proche → écrase et met à jour le depth buffer
  - Plus loin → rejeté
- Garantit un rendu correct **indépendamment de l'ordre de dessin**
---
 
### Étapes programmables vs fixes
 
| Étape | Type | Remarque |
|---|---|---|
| Vertex Shader | **Programmable** | Tu écris la transformation |
| Rasterisation | Fixe | — |
| Interpolation | Fixe | — |
| Fragment Shader | **Programmable** | Tu écris le calcul couleur |
| Depth Test | Fixe (configurable) | Tu choisis la règle de comparaison |
| Framebuffer write | Fixe (configurable) | Tu choisis le blending mode |
 
**Configurable ≠ programmable** : on change un paramètre, on n'écrit pas de code GPU.
 
---
 
### Index Buffer
 
Plutôt que de répéter les vertices partagés entre triangles, on envoie les vertices **une seule fois** dans le VBO et on décrit les triangles via des **indices** :
 
```
Vertices : [A, B, C, D]
Indices  : [0,1,2,  0,2,3]   ← 2 triangles avec 4 vertices au lieu de 6
```
 
Réduit les données transférées vers le GPU → meilleures performances.
 
---
 
### Primitives
 
| Primitive | Description |
|---|---|
| `GL_TRIANGLES` | 3 vertices = 1 triangle |
| `GL_TRIANGLE_STRIP` | N vertices = N-2 triangles (arêtes partagées) |
| `GL_LINES` | 2 vertices = 1 ligne |
| `GL_POINTS` | 1 vertex = 1 point |
 
Le **triangle** est la primitive universelle car ses 3 points définissent toujours **exactement un plan** (toujours planaire). Un quad peut être gauche → ambiguïté géométrique → hardware optimisé pour les triangles uniquement.
 
---
 
### Étapes optionnelles du pipeline moderne
 
| Étape | Position | Usage |
|---|---|---|
| Tessellation | Après vertex shader | Subdivise les triangles (terrain, eau) |
| Geometry Shader | Après vertex shader | Génère/supprime des primitives à la volée |
| Compute Shader | Hors pipeline rendu | GPU généraliste (physique, post-processing) |
 
---
 
### Langages de shaders
 
| API | Langage | Nature |
|---|---|---|
| OpenGL | GLSL | Texte |
| Vulkan | SPIR-V | **Bytecode** |
| Direct3D | HLSL | Texte |
| Metal | MSL | Texte |
 
SPIR-V est souvent compilé depuis GLSL. Des outils comme **SPIRV-Cross** permettent de transpiler entre langages.
 
---
 
### Lien avec la Phase 2
 
La fenêtre (Phase 2) est le **conteneur**. OpenGL (Phase 3) s'y attache via un **contexte OpenGL** pour dessiner dedans.
 
---
 
## ✅ OpenGL
*3 passages effectués — maîtrisé*
 
### Concepts clés
 
**Machine à états** — OpenGL maintient un état global. Tu configures l'état (bind buffer, use program...) puis tu dessines. Le draw call utilise l'état courant. Piège : oublier de changer l'état → draw call silencieusement incorrect.
 
**Contexte OpenGL** — Connexion entre l'application et le GPU. Contient tout l'état OpenGL. S'attache à une fenêtre (créée en Phase 2). En pratique : GLFW ou SDL2 gèrent la création du contexte.
 
**Double buffering** — On dessine dans le back buffer (caché), puis swap vers le front buffer (visible). Évite le scintillement. Dernière opération de chaque frame.
 
**Boucle de rendu** :
```
1. glClear(...)           — effacer le buffer
2. Configurer état         — bind VAO, use program, set uniforms
3. glDrawArrays/Elements  — déclencher le pipeline
4. swap buffers           — afficher
```
 
### Les objets OpenGL
 
Tout est identifié par un **entier (ID)** — handle opaque géré par le driver. Le GC C# ne libère pas les ressources GPU → `glDeleteBuffers`, `glDeleteVertexArrays`, `glDeleteProgram` obligatoires.
 
| Objet | Rôle |
|---|---|
| **VBO** | Données vertices sur le GPU |
| **VAO** | Décrit le vertex layout (stride, offset, type) |
| **Shader Program** | Vertex + fragment shader compilés et liés |
 
### VBO — envoyer des données
 
```csharp
uint vbo;
glGenBuffers(1, out vbo);
glBindBuffer(GL_ARRAY_BUFFER, vbo);
glBufferData(GL_ARRAY_BUFFER, size, data, GL_STATIC_DRAW);
// GL_STATIC_DRAW  = données stables  → mémoire GPU rapide
// GL_DYNAMIC_DRAW = données changeantes
// GL_STREAM_DRAW  = données changent chaque frame
```
 
### VAO — décrire la structure
 
```csharp
glVertexAttribPointer(
    index,      // location dans le shader
    size,       // nb composantes (ex: 3 pour xyz)
    GL_FLOAT,
    false,
    stride,     // taille d'un vertex complet en bytes
    offset      // position de cet attribut dans le vertex
);
```
 
Exemple avec position `(x,y,z)` + UV `(u,v)` :
```
stride = 5 * sizeof(float)
position : offset = 0
UV       : offset = 3 * sizeof(float)
```
 
Le VAO **enregistre** la configuration — binder le VAO suffit à chaque frame, pas besoin de reconfigurer `glVertexAttribPointer`.
 
### Shaders
 
Compilés à **runtime** par le **driver** depuis des strings GLSL :
 
```csharp
uint sh = glCreateShader(GL_VERTEX_SHADER);
glShaderSource(sh, src);
glCompileShader(sh);       // → vérifier erreurs avec glGetShaderInfoLog
 
uint prog = glCreateProgram();
glAttachShader(prog, vertShader);
glAttachShader(prog, fragShader);
glLinkProgram(prog);
glUseProgram(prog);        // doit être actif avant glUniform*
```
 
### Uniforms
 
```csharp
int loc = glGetUniformLocation(program, "time");
glUniform1f(loc, 1.5f);   // doit être appelé APRÈS glUseProgram
```
 
Piège : appeler `glUniform*` avant `glUseProgram` → sans effet (aucun program actif).
 
### Erreurs
 
OpenGL est silencieux — pas d'exceptions :
```csharp
uint err = glGetError();   // 0 = GL_NO_ERROR
```
 
### Limites d'OpenGL → pourquoi Vulkan existe
 
- Machine à états globale → difficile à paralléliser
- Pas de multi-threading natif (1 contexte = 1 thread)
- Compilation shaders à runtime → comportements variables selon drivers
- Driver fait trop de "magie" → performances imprévisibles
### Outils C#
 
| Outil | Rôle |
|---|---|
| **GLFW** | Fenêtre + contexte OpenGL + entrées |
| **GLAD** | Charge les pointeurs de fonctions OpenGL |
| **OpenTK** | Binding C# complet (GLFW + GLAD + types) |
 
---
 
## ✅ Vulkan
*3 passages effectués — maîtrisé*
 
### Philosophie
 
Vulkan (2016) est l'opposé d'OpenGL : **rien n'est fait automatiquement**. Le driver est une fine couche de traduction, pas un assistant. En échange : performances prévisibles, multi-threading natif, zéro magie.
 
| Aspect | OpenGL | Vulkan |
|---|---|---|
| Verbosité | Faible | Élevée |
| Contrôle | Driver décide | Tu décides |
| Multi-threading | Limité | Natif |
| Erreurs | Silencieuses | Validation layers |
| Shaders | GLSL à runtime | SPIR-V offline |
| Pipeline | Assemblé à la volée | Objet immuable |
| Mémoire | Gérée par driver | Gérée par toi |
 
### Séquence d'initialisation
 
```
Instance → Physical Device → Logical Device → Surface → Swapchain
→ Render Pass → Pipeline → Command Pool → Command Buffers → Sync
```
 
### Objets clés
 
**Instance** — Connexion à l'API Vulkan. Déclare extensions et version ciblée.
 
**Physical Device** — Le GPU physique. On l'énumère et on choisit.
 
**Logical Device** — Interface de travail vers le GPU. Expose les fonctions Vulkan utiles. On y déclare les queues et extensions voulues.
 
**Queue / Queue Family** — Canal de soumission de commandes vers le GPU. Chaque queue family a des capacités différentes (Graphics, Compute, Transfer, Present).
 
**Surface** — Lien entre Vulkan et la fenêtre (Phase 2). Plateforme-spécifique (Win32, Wayland, XCB...).
 
**Swapchain** — File de buffers d'affichage. Équivalent du double buffering OpenGL. Tu déclares format, mode de présentation, nombre d'images.
 
**Render Pass** — Description abstraite du rendu : quels attachements (color, depth), comment les charger/stocker, dans quel état avant/après. Pas encore du dessin.
 
**Pipeline** — Objet immuable qui fige shaders + toute la config du rendu. Coûteux à créer, rapide à utiliser. Créé au chargement, caché dans un `VkPipelineCache`.
 
**Command Buffer** — Liste d'instructions enregistrées puis soumises au GPU d'un bloc. Alloué depuis un Command Pool. Permet la préparation multi-thread.
 
### Synchronisation
 
| Primitive | Usage |
|---|---|
| **Semaphore** | GPU ↔ GPU (entre opérations GPU) |
| **Fence** | GPU ↔ CPU (le CPU attend que le GPU ait fini) |
 
### Boucle de rendu
 
```
1. Attendre fence
2. Acquérir image swapchain  →  imageAvailableSemaphore
3. Enregistrer command buffer
4. Soumettre (wait: imageAvailable, signal: renderFinished)
5. Présenter (wait: renderFinished)
```
 
### Descriptors
 
Pas d'uniforms par nom comme OpenGL. Les ressources (buffers, textures) sont référencées via des **Descriptor Sets** et des **binding indices** déclarés dans le shader :
 
```glsl
layout(set = 0, binding = 0) uniform MVP { mat4 model; mat4 view; mat4 proj; };
layout(set = 0, binding = 1) uniform sampler2D albedoTexture;
```
 
### Memory Management
 
Trois étapes : créer la ressource, allouer la mémoire, lier les deux. Types de mémoire :
 
| Type | Propriété | Usage |
|---|---|---|
| `DEVICE_LOCAL` | Rapide, inaccessible CPU | Buffers GPU définitifs |
| `HOST_VISIBLE` | Accessible CPU | Staging buffers |
| `HOST_COHERENT` | Pas besoin de flush | Uploads fréquents |
 
**Staging buffer** : écrire CPU → HOST_VISIBLE, puis copier vers DEVICE_LOCAL via commande de transfert. En pratique : **VMA (Vulkan Memory Allocator)**.
 
### Validation Layers
 
`VK_LAYER_KHRONOS_validation` — intercepte les appels Vulkan et signale les erreurs précisément. Activé en debug uniquement (coût en performance).
 
### Outil C#
 
**Silk.NET** — binding C# complet pour Vulkan (+ OpenGL, windowing).
 
### Limites
 
- Non supporté nativement sur macOS/iOS → **MoltenVK** traduit Vulkan → Metal (avec overhead)
- Pour abstraire Vulkan + Metal + D3D12 : couche HAL maison (Phase 5)
## ✅ Metal / Direct3D 12
*3 passages effectués — maîtrisé*
 
### Philosophie commune
 
Metal (2014) et D3D12 (2015) partagent la même philosophie que Vulkan : contrôle explicite, pas de magie driver. Vulkan s'en est inspiré (ainsi que de Mantle, l'API bas niveau d'AMD). Les concepts sont identiques, la terminologie diffère.
 
### Correspondance terminologique
 
| Concept | Vulkan | Metal | D3D12 |
|---|---|---|---|
| Connexion + interface GPU | Instance + Logical Device | **MTLDevice** (les deux en un) | D3D12CreateDevice |
| File de commandes | Queue | MTLCommandQueue | ID3D12CommandQueue |
| Liste de commandes | Command Buffer | MTLCommandBuffer | ID3D12CommandList |
| Encodeur de commandes | — | MTLRenderCommandEncoder | — |
| Pipeline | VkPipeline | MTLRenderPipelineState | **PSO** |
| Shader binding | Descriptor Sets | Arguments / Argument Buffers | Descriptor Heaps + **Root Signature** |
| Buffers d'affichage | Swapchain | CAMetalLayer + drawable | IDXGISwapChain |
| Transitions ressources | Pipeline Barriers | **Automatique** | Resource Barriers |
| Langage shader | GLSL → SPIR-V | **MSL** (compilé offline → MTLLibrary) | **HLSL** → DXIL |
 
### Metal — spécificités
 
**MTLDevice** — Point d'entrée unique, joue le rôle d'Instance + Logical Device. Une ligne au lieu de cinquante par rapport à Vulkan. Pas de queue families — Metal choisit.
 
**CAMetalLayer** — Remplace la swapchain. À chaque frame, on demande un `drawable`, on dessine dedans, puis `presentDrawable` + `commit`.
 
**Encoders** — Le command buffer est généraliste. Pour chaque type de travail, on crée un encoder spécialisé :
 
| Encoder | Rôle |
|---|---|
| `MTLRenderCommandEncoder` | Draw calls |
| `MTLComputeCommandEncoder` | Compute shaders |
| `MTLBlitCommandEncoder` | Copies mémoire |
 
**Transitions de layout** — Metal les gère automatiquement. Pas d'équivalent des pipeline barriers à écrire.
 
**Architecture tile-based (Apple Silicon)** — Le GPU rend chaque tile indépendamment en mémoire on-chip avant d'écrire en mémoire principale. Les GPU desktop (immediate mode) passent par la mémoire principale à chaque étape → plus de bande passante → moins efficace énergétiquement. D'où les performances/watt exceptionnelles d'Apple Silicon.
 
**Tile Shaders** — Accès direct à la mémoire on-chip entre vertex et fragment shader. Impossible sur GPU desktop.
 
### D3D12 — spécificités
 
**DXGI** — Infrastructure pour l'énumération du hardware et la swapchain. La swapchain est créée avec le `hwnd` Win32 (Phase 2).
 
**Root Signature** — Décrit comment les ressources sont liées aux shaders. Équivalent des Descriptor Set Layouts Vulkan.
 
**PSO (Pipeline State Object)** — Équivalent du VkPipeline. Immuable, créé à l'avance.
 
**Resource Barriers** — Équivalent des pipeline barriers Vulkan. Transition d'état explicite avant chaque changement d'usage :
```
StateBefore = D3D12_RESOURCE_STATE_PRESENT
StateAfter  = D3D12_RESOURCE_STATE_RENDER_TARGET
```
 
**DirectX Raytracing (DXR)** — Ray tracing hardware exposé via D3D12. Nouveaux types de shaders :
 
| Shader | Rôle |
|---|---|
| Ray Generation | Lance les rayons depuis la caméra |
| Closest Hit | Surface touchée |
| Miss | Rayon ne touche rien |
| Any Hit | Chaque intersection (transparence) |
 
### Outils de debug
 
| Plateforme | Outil |
|---|---|
| Vulkan (Linux/Windows) | **RenderDoc** |
| Metal (macOS/iOS) | **GPU Frame Debugger** (Xcode) |
| D3D12 (Windows) | **PIX** |
 
### Portabilité — pourquoi une couche HAL
 
Aucun moteur sérieux n'écrit trois fois le même code de rendu. Une **couche HAL (Hardware Abstraction Layer)** expose une API unifiée et traduit vers Vulkan/D3D12/Metal selon la plateforme. Avantages : pas de duplication, maintenabilité (nouvelle API = modifier uniquement le HAL). Exemples : bgfx, LLGL, couches internes d'Unreal/Unity. → **Phase 5**.
 
---
 
## Récapitulatif Phase 3
 
```
Pipeline de rendu  →  ce que le GPU fait (universel)
OpenGL             →  machine à états, facile d'accès, héritage
Vulkan             →  contrôle total, cross-platform, verbeux
Metal              →  Apple uniquement, concis, tile-based
D3D12              →  Windows/Xbox, verbeux comme Vulkan, DXR
```
 
---
 
*Dernière mise à jour : Phase 3 complète ✓*