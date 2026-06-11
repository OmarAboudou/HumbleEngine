# Roadmap — Extraction X11 et Wayland

Objectif : sortir X11 et Wayland du projet Linux dans leurs propres projets,
puis corriger Vulkan, OpenGL et Linux en conséquence.

## Étapes

- [x] 1. Créer `HumbleEngine.X11` → Core
  - X11Native, X11Window, X11WindowBackend
- [x] 2. Créer `HumbleEngine.Wayland` → Core
  - WaylandWindowBackend (stub)
- [x] 3. Corriger `HumbleEngine.Vulkan`
  - Référencer X11 + Wayland
  - `CompatibleWindowBackends : IReadOnlyList<Type>` avec typeof(X11WindowBackend) et typeof(WaylandWindowBackend)
  - `Initialize(IGraphicsSurface)` à la place de `TryInitialize`
- [x] 4. Corriger `HumbleEngine.OpenGL`
  - Idem Vulkan
- [x] 5. Corriger `HumbleEngine.Linux`
  - `LinuxOS` hérite de `DesktopOS` (plus `OS`)
  - Supprimé `Surfaces/` inline (X11, Wayland, Vulkan, OpenGL)
  - Référence HumbleEngine.X11, HumbleEngine.Wayland, HumbleEngine.Vulkan, HumbleEngine.OpenGL
  - `AvailableWindowBackends` et `DefaultWindowBackend` à la place de `AvailableSurfaceBackends`
- [x] 6. Build solution complète — 0 erreur, 0 avertissement ✅

---

## Résultat

Tous les projets compilent. Dépendances finales :

```
Core      ← (aucune)
X11       ← Core
Wayland   ← Core
Vulkan    ← Core + X11 + Wayland
OpenGL    ← Core + X11 + Wayland
Linux     ← Core + X11 + Wayland + Vulkan + OpenGL
```

*Tâche terminée*
