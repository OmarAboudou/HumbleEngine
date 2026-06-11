# Roadmap — Implémentation VulkanGraphicsBackend

Objectif : remplacer le stub Vulkan par un backend réel, jusqu'au cycle de frame
complet (clear screen présenté à l'écran), à parité avec le backend OpenGL.

Découpage en trois blocs — chaque bloc compile, est testé et validé avant de passer au suivant.

## Blocs

- [x] **Bloc 1 — Instance + sélection du GPU** ✅
  - `VulkanNative` (P/Invoke `libvulkan.so.1`) ; `Initialize()` : VkInstance + classement des GPUs
  - Tests d'intégration (7)

- [x] **Bloc 2 — Surface + device logique + swapchain** ✅
  - VkSurfaceKHR (Xlib ou Wayland selon `IWindow.Backend`), queue family graphics+present,
    `vkCreateDevice`, swapchain
  - Ajouts à Core : `IWindow.Backend`, `IWindow.Width`/`Height`
  - Fallback multi-GPU : sur ce laptop hybride, la NVIDIA (driver 535) ne peut pas créer de
    swapchain Wayland → repli sur l'iGPU Intel ; sur X11 la NVIDIA fonctionne
  - Tests d'intégration (12 au total)

- [x] **Bloc 3 — Cycle de frame** ✅
  - Command buffer, synchronisation (1 frame in flight), `BeginFrame`/`EndFrame`/`Present` avec clear
  - Recréation de la swapchain au resize
  - Ajout à Core : `INativeWindowHandle.NotifyRendererAttached()` (cohabitation buffer wl_shm / swapchain)
  - Sandbox : `dotnet run -- [Wayland|X11] [Vulkan|OpenGL]`
  - Tests d'intégration (14 au total)

---

## Résultat

Le backend Vulkan est à parité avec OpenGL : fenêtre clear gris foncé, X11 et Wayland,
recréation de swapchain au resize, fallback multi-GPU. Prochaine étape graphique (hors
roadmap) : pipeline + shaders pour dessiner un triangle.

*Tâche terminée*
