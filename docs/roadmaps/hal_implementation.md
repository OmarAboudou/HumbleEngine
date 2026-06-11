# Roadmap — Implémentation HAL (Phase 5)

## Structure cible des projets

```
HumbleEngine.Core      → abstractions uniquement
HumbleEngine.X11       → Core
HumbleEngine.Wayland   → Core
HumbleEngine.Win32     → Core                          (pas encore créé)
HumbleEngine.Cocoa     → Core                          (pas encore créé)
HumbleEngine.Vulkan    → Core + X11 + Wayland + Win32  (Win32 futur)
HumbleEngine.OpenGL    → Core + X11 + Wayland + Win32  (Win32 futur)
HumbleEngine.Metal     → Core + Cocoa                  (pas encore créé)
HumbleEngine.D3D12     → Core + Win32                  (pas encore créé)
HumbleEngine.Linux     → Core + X11 + Wayland + Vulkan + OpenGL
HumbleEngine.Windows   → Core + Win32 + Vulkan + D3D12 (pas encore créé)
HumbleEngine.macOS     → Core + Cocoa + Metal           (pas encore créé)
```

---

## État des projets

| Projet               | Statut          | Notes                                                    |
|----------------------|-----------------|----------------------------------------------------------|
| HumbleEngine.Core    | ✅ Fait         | OS, DesktopOS, MobileOS, toutes les interfaces HAL       |
| HumbleEngine.X11     | ✅ Fait         | X11Native, X11Window, X11WindowBackend                   |
| HumbleEngine.Wayland | ✅ Fait         | WaylandWindowBackend (stub)                              |
| HumbleEngine.Vulkan  | ✅ Fait         | CompatibleWindowBackends (types), Initialize             |
| HumbleEngine.OpenGL  | ✅ Fait         | Idem Vulkan                                              |
| HumbleEngine.Linux   | ✅ Fait         | LinuxOS hérite DesktopOS, Surfaces/ inline supprimé      |
| HumbleEngine.Windows | 🔲 Pas commencé |                                                          |
| HumbleEngine.macOS   | 🔲 Pas commencé |                                                          |

---

## Tâches en cours

- ✅ Extraction X11/Wayland terminée — voir `extraction_x11_wayland.md`

## Prochaines tâches (après extraction)

- [ ] Implémenter VulkanGraphicsBackend réel
- [ ] Implémenter OpenGLGraphicsBackend réel
- [ ] Créer HumbleEngine.Windows
- [ ] Créer HumbleEngine.macOS

---

*Dernière mise à jour : X11 et Wayland créés, Vulkan/OpenGL/Linux à corriger*
