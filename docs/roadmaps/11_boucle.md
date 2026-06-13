# Roadmap — Le découplage pompe/rendu

> **DIFFÉRÉE (2026-06-13)** — décision d'Omar : le bug ne touche que le
> **bi-fenêtre dans un seul processus** ; le Sandbox a été **réduit à une
> fenêtre** (mono-Wayland), et la refonte attend son **vrai client** : l'éditeur
> / le vrai multi-fenêtre (ère WindowNode). Ce fichier garde le diagnostic et les
> options pour ce moment-là. Pas la roadmap en cours.

Objectif : la **pompe d'événements** (input, presse-papiers, fenêtre) doit être
servie **promptement, quel que soit l'état du rendu**. Aujourd'hui une fenêtre
réduite/occultée fige ou laggue la boucle — et avec elle l'input et le clipboard
des *autres* fenêtres.

Découpage en blocs — chaque bloc compile, **se voit** (Sandbox) et est validé avant le suivant.

## Le diagnostic (porté par la saga presse-papiers, 2026-06-13)

La boucle est **mono-thread** et **cadencée par le rendu** :
`while (a.Step() && b.Step())`, où `Step` = `PollEvents` **puis** `onFrame`
(rendre). Quand le compositeur cesse de composer une fenêtre, les appels **WSI
Vulkan bloquent** et gèlent la pompe (donc le clipboard, l'input) :

- `vkAcquireNextImageKHR` (FIFO, pas d'image libre) — **borné à 16 ms** (fait).
- `vkQueuePresentKHR` (throttle quand occulté).
- **`vkDeviceWaitIdle` dans `RecreateSwapchain`** quand l'acquire renvoie
  `OutOfDate` sur une fenêtre réduite (extent 0×0) — attend un *present* coincé.

**Mitigation déjà en place** (commit « Serve the clipboard over Wayland ») :
`BeginFrame → bool` (frame sautée si pas présentable), acquire borné,
`IWindow.IsSuspended` (Wayland libdecor SUSPENDED, X11 VisibilityNotify) → `Step`
saute le rendu d'une fenêtre cachée. **Insuffisant** : la détection ne se
déclenche pas pour l'occultation par une appli Wayland sous Xwayland, et les
autres appels WSI bloquent encore. Le whack-a-mole appel-par-appel ne suffit pas.

**Portée réelle** : n'affecte que le **bi-fenêtre dans un seul processus** (le
Sandbox). Une appli **mono-fenêtre** (le cap produit) ne le rencontre jamais. Le
presse-papiers lui-même est **correct** (prouvé par logs). C'est un confort
multi-fenêtre, pas un blocage produit — mais c'est le bon moment de poser la
bonne architecture de boucle (elle croisera l'ère WindowNode, roadmap 09).

## Le fork à trancher quand on reprend : où vit le rendu vs la pompe

Décision structurante, qui touche la philosophie (« l'appli possède sa boucle,
pas d'`EngineContext`, pas de threads cachés ») :

- **Boucle pilotée par les fd (mono-thread)** : un seul thread, `poll()` des fd
  d'affichage (X11 + `wl_display`) avec timeout ; pomper une fenêtre quand son fd
  est prêt ; rendre sur un timer en **sautant les fenêtres non-présentables**.
  Garde le modèle simple, mais exige de rendre **tous** les points bloquants du
  rendu sautables (present, `vkDeviceWaitIdle` au recreate), pas juste l'acquire.
  Fidèle à « l'appli possède sa boucle ».
- **Thread de rendu par fenêtre** : le thread principal pompe tous les événements ;
  chaque fenêtre a son thread de rendu (BeginFrame/render/present bloquant isolé).
  Ne gèle jamais la pompe ; le trio fenêtre↔renderer↔arbre est déjà indépendant.
  Coût : thread-safety de l'arbre (input écrit côté main, rendu lit côté render
  thread), synchronisation. C'est le **render thread à la Godot** déjà noté en
  mémoire comme direction future — pas qu'un correctif, un pas vers l'ère WindowNode.
- **Présentation non-bloquante seule** : rester mono-thread cadencé par le rendu,
  mais garantir qu'aucun WSI ne bloque (acquire borné fait + MAILBOX au lieu de
  FIFO + sauter le recreate à extent 0×0). Plus petit, mais MAILBOX consomme plus
  et n'est pas garanti partout — et ça reste du colmatage point par point.

## Blocs

*Différée — voir l'en-tête. Le fork ci-dessus est le point de départ quand le
client (éditeur / vrai multi-fenêtre) arrive.*
