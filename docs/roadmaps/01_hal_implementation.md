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

| Projet                    | Statut          | Notes                                                                              |
|---------------------------|-----------------|------------------------------------------------------------------------------------|
| HumbleEngine.Core         | ✅ Fait         | OS, DesktopOS, MobileOS, toutes les interfaces HAL, XML doc                        |
| HumbleEngine.X11          | ✅ Fait         | X11Native, X11Window (Motif hints pour borderless), X11WindowBackend               |
| HumbleEngine.Wayland      | ✅ Fait         | libdecor intégré (CSD GNOME) ; fallback XDG brut pour borderless/sans libdecor      |
| HumbleEngine.Vulkan       | ✅ Fait         | Instance, fallback multi-GPU, surface, device, swapchain, cycle de frame — voir `03_vulkan_implementation.md` |
| HumbleEngine.OpenGL       | ✅ Fait         | GLX context, BeginFrame/EndFrame/Present via P/Invoke libGL                        |
| HumbleEngine.Linux        | ✅ Fait         | LinuxOS public, Wayland en premier, X11 en fallback                                |
| HumbleEngine.Sandbox      | ✅ Fait         | Sélection par arguments : `-- [Wayland\|X11] [Vulkan\|OpenGL]`                      |
| HumbleEngine.Tests        | ✅ Fait         | Tests unitaires — FakeOS, aucune dépendance à un display                           |
| HumbleEngine.Tests.Linux  | ✅ Fait         | Tests d'intégration — X11, GLX, Wayland (libdecor + XDG brut), cycle frame complet |
| HumbleEngine.Windows      | 🔲 Pas commencé |                                                                                    |
| HumbleEngine.macOS        | 🔲 Pas commencé |                                                                                    |

---

## Prochaines tâches

- [x] Intégrer `libdecor` dans `WaylandWindow` pour les décorations sur GNOME Wayland
- [x] Implémenter `VulkanGraphicsBackend` réel — roadmap dédiée : `03_vulkan_implementation.md` ✅
- [ ] Créer `HumbleEngine.Windows` (Win32 + D3D12)
- [ ] Créer `HumbleEngine.macOS` (Cocoa + Metal)
