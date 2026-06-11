# 🗺️ Roadmap générale — Programmation bas-niveau en C#
 
> **Objectif** : Être capable de pratiquer et appliquer tout en comprenant comment ça marche sous le capot.
> **Langage** : C# | **Cible initiale** : Linux → Windows → macOS → Mobile
 
---
 
## Progression globale
 
```
Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5
```
 
---
 
## Phase 1 — Fondations système
> *Les briques invisibles sur lesquelles tout repose*
 
**Statut** : 🔲 Non commencé
 
### Concepts clés
- [ ] Mémoire : stack, heap, pointeurs, gestion manuelle vs GC
- [ ] OS : processus, threads, appels système (syscalls)
- [ ] Interop C# ↔ natif : P/Invoke, unsafe, Span\<T\>
---
 
## Phase 2 — Windowing & affichage natif
> *Créer une fenêtre depuis zéro, sans framework*
 
**Statut** : 🔲 Non commencé
 
### Concepts clés
- [ ] API Linux : X11, Wayland (xdg-shell)
- [ ] API Windows : Win32 (HWND, WndProc, message loop)
- [ ] API macOS : Cocoa / AppKit (NSWindow)
---
 
## Phase 3 — Programmation graphique
> *Dessiner des pixels, comprendre le GPU*
 
**Statut** : 🔲 Non commencé
 
### Concepts clés
- [ ] Pipeline de rendu : vertex → rasterisation → fragment
- [ ] OpenGL (cross-platform, bon point d'entrée)
- [ ] Vulkan (contrôle total, verbeux)
- [ ] Metal (macOS/iOS) · Direct3D 12 (Windows)
---
 
## Phase 4 — Architecture plugin
> *Concevoir un système extensible et modulaire*
 
**Statut** : 🔲 Non commencé
 
### Concepts clés
- [ ] Chargement dynamique : DLL / .so / .dylib
- [ ] Interfaces & contrats : versioning, isolation, ABI
- [ ] Patterns : plugin registry, event bus, hot-reload
- [ ] Sécurité : sandboxing, permissions plugin
---
 
## Phase 5 — Multi-plateforme & production
> *Unifier tout ce qui précède sur toutes les cibles*
 
**Statut** : 🔄 En cours — Bloc 1 (HAL) terminé
 
### Concepts clés
- [x] Abstraction HAL : couche qui cache les API natives
- [ ] CI/CD cross-platform, packaging (Flatpak, MSIX, .pkg)
- [ ] Mobile : iOS (Metal + UIKit) · Android (Vulkan + JNI)
---
 
## Journal de progression
 
| Date | Phase | Concept vu | Niveau atteint |
|------|-------|------------|----------------|
| 2026-06-11 | Phase 5 | HAL — interfaces, backends, OS, cycle de vie | Maîtrisé (3 passes) |
 
---
 
*Fichier maintenu au fil des discussions du projet.*