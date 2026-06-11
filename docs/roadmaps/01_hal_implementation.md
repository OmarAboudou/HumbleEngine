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

| Projet                    | Statut          | Notes                                                              |
|---------------------------|-----------------|--------------------------------------------------------------------|
| HumbleEngine.Core         | ✅ Fait         | OS, DesktopOS, MobileOS, toutes les interfaces HAL, XML doc        |
| HumbleEngine.X11          | ✅ Fait         | X11Native, X11Window, X11WindowBackend                             |
| HumbleEngine.Wayland      | ✅ Fait         | WaylandWindowBackend (stub)                                        |
| HumbleEngine.Vulkan       | ✅ Fait         | Stub — CompatibleWindowBackends déclaré, Initialize non implémenté |
| HumbleEngine.OpenGL       | ✅ Fait         | GLX context, BeginFrame/EndFrame/Present via P/Invoke libGL        |
| HumbleEngine.Linux        | ✅ Fait         | LinuxOS public, enregistrement explicite via OS.Register           |
| HumbleEngine.Sandbox      | ✅ Fait         | Projet exécutable de test (X11 + OpenGL)                           |
| HumbleEngine.Tests        | ✅ Fait         | Tests unitaires — FakeOS, aucune dépendance à un display           |
| HumbleEngine.Tests.Linux  | ✅ Fait         | Tests d'intégration — X11, GLX, cycle frame complet                |
| HumbleEngine.Windows      | 🔲 Pas commencé |                                                                    |
| HumbleEngine.macOS        | 🔲 Pas commencé |                                                                    |

---

## Décisions d'architecture notables

- `INativeWindowHandle` expose `GetConnectionHandle()` (Display* sur X11) — pas d'interface X11-spécifique séparée.
- `OS.Register(new LinuxOS())` est la responsabilité explicite du `Program.cs` — pas de `[ModuleInitializer]` magique.
- `IGraphicsBackend.Supports` est une méthode d'interface par défaut, overridable pour des vérifications runtime.
- Les capacités optionnelles (`IRayTracingCapability`, `ITileShadingCapability`) sont détectées via le pattern `is`.

## Prochaines tâches

- [ ] Implémenter VulkanGraphicsBackend réel
- [ ] Créer HumbleEngine.Windows (Win32 + D3D12)
- [ ] Créer HumbleEngine.macOS (Cocoa + Metal)
