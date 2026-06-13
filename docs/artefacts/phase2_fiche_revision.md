# Fiche de révision — Phase 2 : Windowing & affichage natif
 
> Progression : ██████████ Phase 2 complète ✓
 
---
 
## ✅ Patron commun du windowing
*1 passage effectué — acquis*
 
### Le cycle universel
 
Toute API de windowing suit le même schéma en 4 temps :
 
```
1. ENREGISTRER  — Décrire à l'OS le type de fenêtre
2. CRÉER        — Instancier la fenêtre
3. BOUCLER      — Maintenir le programme en vie, attendre les événements
4. RÉAGIR       — Traiter chaque événement
```
 
**Pourquoi une boucle ?** Sans elle, le programme se terminerait immédiatement après la création de la fenêtre. La boucle le maintient en vie.
 
**La file d'événements** — L'OS accumule les événements (clics, touches, redimensionnement, fermeture...) dans une queue. La boucle les dépile un par un.
 
**Le gestionnaire d'événements** s'appelle différemment selon les API :
- Win32 : `WndProc`
- X11 : un `switch` sur le type d'événement
- Cocoa : méthodes déléguées `NSWindowDelegate`
### Pièges
- La boucle d'événements *est* du polling (elle dépile activement). `GetMessage` (Win32) est une variante bloquante qui suspend le thread jusqu'au prochain événement.
- Ne pas traiter l'événement de fermeture = la fenêtre refuse de se fermer.
---
 
## ✅ X11 (Linux)
*3/3 passages effectués — maîtrisé*
 
### Architecture
 
X11 est **explicitement client/serveur**. Le serveur X (Xorg) gère les écrans et les entrées. Ton programme est un client qui lui envoie des commandes via socket.
 
Conséquence : tu dois ouvrir une connexion explicite ET t'abonner aux événements voulus. Par défaut, X11 ne t'envoie rien.
 
Différence avec Win32 : Win32 utilise un modèle **implicite** (le processus est automatiquement associé au système d'affichage). X11 peut même afficher sur une machine distante via le réseau.
 
### Identifiants clés
 
| Concept | X11 | Win32 |
|---|---|---|
| Identifiant de fenêtre | `XID` (entier) | `HWND` (handle opaque) |
| Connexion au serveur | `Display*` (explicite) | implicite |
 
### Squelette minimal
 
```csharp
IntPtr display = XOpenDisplay(null);       // Connexion au serveur
int screen = XDefaultScreen(display);
 
ulong window = XCreateSimpleWindow(        // Crée la fenêtre côté serveur (invisible)
    display, XRootWindow(display, screen),
    100, 100, 800, 600, 1,
    XBlackPixel(display, screen),
    XWhitePixel(display, screen)
);
 
XSelectInput(display, window,              // Abonnement aux événements
    EventMask.ExposureMask | EventMask.KeyPressMask);
 
XMapWindow(display, window);               // Rend la fenêtre visible
 
// Fermeture propre via le gestionnaire de fenêtres
IntPtr wmDelete = XInternAtom(display, "WM_DELETE_WINDOW", false);
XSetWMProtocols(display, window, ref wmDelete, 1);
 
XEvent ev = new XEvent();
while (true)
{
    XNextEvent(display, ref ev);           // Bloquant
    switch (ev.type)
    {
        case XEventType.Expose:     /* redessiner */ break;
        case XEventType.KeyPress:   goto done;
        case XEventType.ClientMessage:
            if (ev.xclient.data.l[0] == (long)wmDelete) goto done;
            break;
    }
}
done:
XDestroyWindow(display, window);
XCloseDisplay(display);
```
 
### Fonctions clés
 
| Fonction | Rôle |
|---|---|
| `XOpenDisplay(null)` | Ouvre la connexion au serveur X (variable `DISPLAY`) |
| `XCreateSimpleWindow` | Crée la fenêtre côté serveur (invisible) |
| `XSelectInput` | S'abonne aux événements voulus |
| `XMapWindow` | Rend la fenêtre visible |
| `XNextEvent` | Attend le prochain événement (bloquant) |
| `XInternAtom` | Récupère un atome (identifiant nommé X11) |
| `XSetWMProtocols` | Enregistre les protocoles WM supportés |
 
### Événements importants
 
| Événement | Déclencheur |
|---|---|
| `Expose` | La fenêtre doit se (re)dessiner |
| `KeyPress` | Touche pressée |
| `ClientMessage` | Message du gestionnaire de fenêtres (ex: ✕) |
 
### Pièges
- `XCreateSimpleWindow` crée la fenêtre côté **serveur** (pas côté client). `XMapWindow` ne crée rien — elle la rend visible.
- Sans `XSelectInput`, aucun événement n'est reçu.
- Cliquer sur ✕ sans `WM_DELETE_WINDOW` = fenêtre bloquée ou connexion coupée brutalement par le serveur.
- `XOpenDisplay` peut retourner null — toujours vérifier.
---
 
## ✅ Wayland (Linux)
*3/3 passages effectués — maîtrisé*
 
### Architecture
 
Wayland **fusionne serveur et compositeur** en une seule entité. Ton app parle directement au compositeur (GNOME, KDE, sway...).
 
```
X11 :     ton app → serveur X → compositeur → écran
Wayland : ton app → compositeur → écran
```
 
Différences clés vs X11 :
- Ton app **ne connaît pas sa position absolue** sur l'écran (décision du compositeur, choix de sécurité)
- Les requêtes sont **bufferisées** — rien n'est envoyé sans `wl_surface_commit`
- Protocole **extensible** : base minimaliste + extensions (xdg-shell, zwlr_layer_shell...)
### Objets clés
 
| Objet | Rôle |
|---|---|
| `wl_display` | Connexion au compositeur |
| `wl_registry` | Annuaire des globaux disponibles |
| `wl_compositor` | Fabrique de surfaces |
| `wl_surface` | Surface nue (buffer de pixels, sans rôle) |
| `xdg_wm_base` | Point d'entrée xdg-shell |
| `xdg_surface` | Surface avec rôle xdg |
| `xdg_toplevel` | Fenêtre applicative standard (barre de titre, boutons...) |
 
### Hiérarchie de création
 
```
wl_display → wl_registry → wl_compositor → wl_surface
                         → xdg_wm_base  → xdg_surface → xdg_toplevel
```
 
### Rôles de wl_surface
 
| Rôle | Extension | Usage |
|---|---|---|
| Fenêtre normale | `xdg_toplevel` | Application classique |
| Popup / menu | `xdg_popup` | Menu contextuel |
| Sous-surface | `wl_subsurface` | Overlay enfant d'une surface |
| Overlay système | `zwlr_layer_shell` | Barres de tâches, HUD |
 
### Fonctions clés
 
| Fonction | Rôle |
|---|---|
| `wl_display_connect(null)` | Connexion au compositeur (`$WAYLAND_DISPLAY`) |
| `wl_display_get_registry` | Récupère le registre des globaux |
| `wl_registry_add_listener` | Enregistre les callbacks de découverte |
| `wl_display_roundtrip` | Flush + attend réponse complète (init) |
| `wl_display_dispatch` | Flush + traite événements (boucle) |
| `wl_registry_bind` | S'abonne à un global par nom |
| `wl_compositor_create_surface` | Crée une surface nue |
| `wl_surface_commit` | Valide et envoie l'état courant au compositeur |
| `xdg_surface_ack_configure` | Acquitte un configure (obligatoire) |
 
### Listeners
 
Chaque objet a ses propres listeners — structs de pointeurs de fonctions :
 
```csharp
[StructLayout(LayoutKind.Sequential)]
struct WlRegistryListener
{
    public IntPtr global;         // global disponible
    public IntPtr global_remove;  // global retiré
}
```
 
Les delegates **doivent être épinglés** avec `GCHandle.Alloc(..., GCHandleType.Pinned)` sinon le GC les collecte → crash.
 
### Événement critique : xdg configure
 
Le compositeur envoie `configure` pour imposer une taille. Tu **dois** répondre :
 
```csharp
[UnmanagedCallersOnly]
static void OnXdgSurfaceConfigure(IntPtr data, IntPtr xdgSurface, uint serial)
{
    xdg_surface_ack_configure(xdgSurface, serial);
    wl_surface_commit(surface);
}
```
 
Ignorer ce message = compositeur peut tuer la surface.
 
### Pièges
 
| Piège | Conséquence |
|---|---|
| Pas de `wl_surface_commit` | Rien n'est envoyé au compositeur |
| Delegate collecté par GC | Crash au premier appel du callback |
| `ack_configure` ignoré | Surface considérée bloquée, peut être tuée |
| `WlInterface` mal déclarée | `wl_registry_bind` retourne un objet invalide |
 
### En pratique
 
P/Invoke Wayland brut est très verbeux. En production : **NWayland** (binding généré) ou **SDL2** (abstraction X11/Wayland/Win32 transparente).
 
---
 
## ✅ Win32 (Windows)
*3/3 passages effectués — maîtrisé*
 
### Architecture
 
Win32 utilise un modèle **implicite** — pas de connexion explicite au serveur d'affichage. L'association se fait via `HINSTANCE` (handle vers ton exécutable).
 
Chaque fenêtre est associée à une `WndProc` — Windows *pousse* les événements vers ta fonction (modèle push), contrairement à X11 qui est en polling.
 
### Cycle de création
 
```
1. GetModuleHandle(null)  →  récupère HINSTANCE (handle de ton .exe)
2. RegisterClassEx        →  enregistre le moule WNDCLASSEX (avec WndProc)
3. CreateWindowEx         →  instancie une fenêtre → HWND
4. ShowWindow + UpdateWindow → rend la fenêtre visible
5. Boucle :
      GetMessage          →  attend un message (bloquant)
      TranslateMessage    →  WM_KEYDOWN → WM_CHAR (gère dispositions clavier, touches mortes)
      DispatchMessage     →  appelle WndProc
```
 
### WndProc
 
```csharp
[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
{
    switch (msg)
    {
        case WM_DESTROY:
            PostQuitMessage(0);
            return IntPtr.Zero;
        case WM_PAINT:
            PAINTSTRUCT ps;
            IntPtr hdc = BeginPaint(hwnd, out ps);
            EndPaint(hwnd, ref ps);   // obligatoire — valide la région invalide
            return IntPtr.Zero;
        default:
            return DefWindowProc(hwnd, msg, wParam, lParam);
    }
}
```
 
### Pointeur de fonction stable
 
```csharp
unsafe
{
    wc.lpfnWndProc = (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, IntPtr, IntPtr>)&WndProc;
}
```
 
### Messages importants
 
| Message | Déclencheur |
|---|---|
| `WM_CLOSE` | Clic sur ✕ |
| `WM_DESTROY` | Fenêtre détruite → appeler `PostQuitMessage` |
| `WM_QUIT` | `GetMessage` retourne 0 → boucle s'arrête |
| `WM_PAINT` | Zone invalide à redessiner |
| `WM_SIZE` | Redimensionnement (LOWORD=largeur, HIWORD=hauteur dans lParam) |
| `WM_KEYDOWN` | Touche pressée (keycode) |
| `WM_CHAR` | Caractère (après `TranslateMessage`) |
 
### Chaîne de fermeture
 
```
clic ✕ → WM_CLOSE → DefWindowProc → DestroyWindow
       → WM_DESTROY → PostQuitMessage (ton code)
       → WM_QUIT → GetMessage retourne 0 → boucle s'arrête
```
 
### Styles de fenêtre
 
| Style | Effet |
|---|---|
| `WS_OVERLAPPEDWINDOW` | Fenêtre standard (barre de titre, bordure, redimensionnable) |
| `WS_POPUP` | Sans décoration (plein écran, splashscreen) |
| `WS_CHILD` | Fenêtre enfant |
| `WS_EX_TOPMOST` | Toujours au premier plan (style étendu) |
 
### Pièges
 
| Piège | Conséquence |
|---|---|
| Oublier `DefWindowProc` | Fenêtre ne répond plus au redimensionnement, raccourcis système... |
| `WM_PAINT` sans `BeginPaint`/`EndPaint` | Boucle infinie de `WM_PAINT` |
| Mauvaise convention d'appel | Arguments dans le mauvais ordre, pile corrompue, crash |
| `Marshal.GetFunctionPointerForDelegate` pour `lpfnWndProc` | Pointeur instable, GC peut déplacer le delegate |
| LOWORD/HIWORD inversés dans `WM_SIZE` | Largeur et hauteur échangées silencieusement |
| `CharSet` non spécifié | Appelle la version ANSI (`A`) au lieu d'Unicode (`W`) |
 
---
 
## ✅ Cocoa / AppKit (macOS)
*3/3 passages effectués — maîtrisé*
 
### Architecture
 
Cocoa repose sur **Objective-C** — tout appel de méthode est un envoi de message via `objc_msgSend`. Depuis C#, tu ne peux pas appeler l'API directement — tu simules le système de messagerie ObjC via P/Invoke.
 
Cocoa gère sa propre boucle d'événements (`[NSApp run]`) — tu n'as pas à l'écrire.
 
### Briques de base
 
```csharp
const string ObjC   = "/usr/lib/libobjc.dylib";
const string AppKit = "/System/Library/Frameworks/AppKit.framework/AppKit";
 
[DllImport(ObjC)] static extern IntPtr objc_getClass(string name);
[DllImport(ObjC)] static extern IntPtr sel_registerName(string name);
[DllImport(ObjC)] static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr sel);
// + surcharges par signature (voir pièges)
```
 
`sel_registerName` intern la chaîne — deux appels avec `"setTitle:"` retournent le même pointeur. ObjC compare les sélecteurs par pointeur (rapide), pas par string.
 
### Cycle de création
 
```
1. [NSApplication sharedApplication]  →  objet app unique
2. setActivationPolicy: 0             →  app normale (icône dock)
3. [NSWindow alloc] + initWithContentRect:styleMask:backing:defer:
4. setTitle: + makeKeyAndOrderFront:  →  titre + affichage
5. Créer classe délégué ObjC          →  class_addMethod + objc_registerClassPair
6. setDelegate:                       →  attacher le délégué à la fenêtre
7. [NSApp run]                        →  boucle événements (bloquant)
```
 
### alloc / init
 
La création d'un objet ObjC est toujours en deux étapes :
- **`alloc`** — alloue la mémoire brute (champs à zéro)
- **`init`** — initialise l'objet (valeurs par défaut, ressources)
```csharp
IntPtr obj = objc_msgSend(objc_msgSend(cls, alloc), init);
// équivalent : new MyClass() en C#
```
 
### Structs géométriques
 
```csharp
[StructLayout(LayoutKind.Sequential)]
struct NSRect
{
    public NSPoint origin;  // bas-gauche (⚠️ origine inversée vs Win32/X11)
    public NSSize  size;
}
```
 
**Origine (0,0) = bas-gauche** sur macOS — contrairement à Win32 et X11 (haut-gauche).
 
### StyleMask
 
```csharp
[Flags]
enum NSWindowStyleMask : ulong
{
    Borderless       = 0,
    Titled           = 1 << 0,
    Closable         = 1 << 1,
    Miniaturizable   = 1 << 2,
    Resizable        = 1 << 3,
    FullScreen       = 1 << 14,
}
// Fenêtre standard = 0xF = Titled | Closable | Miniaturizable | Resizable
```
 
### Délégué ObjC depuis C#
 
```csharp
// Créer une classe ObjC héritant de NSObject
IntPtr cls = objc_allocateClassPair(objc_getClass("NSObject"), "MyDelegate", IntPtr.Zero);
 
// Ajouter une méthode
class_addMethod(cls, sel_registerName("windowWillClose:"), onClose, "v@:@");
// "v@:@" = void, self (id), SEL, un objet
 
objc_registerClassPair(cls);
 
// Callback
[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
static void OnClose(IntPtr self, IntPtr sel, IntPtr notif) { ... }
```
 
### Type encoding ObjC
 
| Char | Type |
|---|---|
| `v` | void |
| `@` | objet ObjC (id / self) |
| `:` | SEL (sélecteur, toujours 2e arg implicite) |
 
### Pièges
 
| Piège | Conséquence |
|---|---|
| Mauvaise surcharge `objc_msgSend` | Valeurs corrompues, crash possible |
| `objc_msgSend_stret` sur ARM64 | N'existe pas sur Apple Silicon — utiliser `objc_msgSend` |
| UI depuis thread secondaire | Comportement indéfini (crash, affichage corrompu) |
| Oublier `drain` sur NSAutoreleasePool | Fuite mémoire |
| Coordonnées haut-gauche portées depuis Win32/X11 | Fenêtre positionnée au mauvais endroit |
 
### Autorelease pool
 
```csharp
IntPtr pool = objc_msgSend(objc_msgSend(objc_getClass("NSAutoreleasePool"), alloc),
                           sel_registerName("init"));
// ... code ...
objc_msgSend_void(pool, sel_registerName("drain"));
```
 
---
 
*Dernière mise à jour : Phase 2 complète ✓*