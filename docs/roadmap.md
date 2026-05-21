# Roadmap d'apprentissage — HumbleEngine

## Vue d'ensemble

L'apprentissage est structuré en **deux axes** étudiés en parallèle, selon une **approche en spirale** : chaque axe est d'abord couvert en vulgarisation, puis revisité avec une précision croissante.

```
Tour 1 (Vulgarisation) → Tour 2 (Intermédiaire) → Tour 3 (Technique) → Synthèse ✅
```

---

## Axe 1 — Architectures de moteurs de jeu

> Comprendre comment les grands moteurs existants sont structurés globalement.

| # | Moteur | Architecture | Statut |
|---|--------|-------------|--------|
| 1 | **Godot** | Node Tree | ✅ Tour 3 terminé |
| 2 | **Unity** | GameObject + Components | ✅ Tour 3 terminé |
| 3 | **Unreal** | Actor / Component hybride | ✅ Tour 3 terminé |
| 4 | **Bevy** | ECS pur | ✅ Tour 3 terminé |

---

## Axe 2 — Systèmes UI & frameworks applicatifs

> Comprendre comment les grands systèmes UI sont conçus pour en extraire les meilleures idées.

| # | Système | Philosophie | Statut |
|---|---------|------------|--------|
| 1 | **HTML + CSS** | Arborescence déclarative + règles de style séparées | ✅ Tour 3 terminé |
| 2 | **WPF / XAML** | Version native Microsoft de HTML+CSS, en C# | ✅ Tour 3 terminé |
| 3 | **Flutter** | Tout est Widget, rendu pixel par pixel (inspiré de React) | ✅ Tour 3 terminé |
| 4 | **Qt** | Signals & Slots, layouts cross-platform, très influent | ✅ Tour 3 terminé |

---

## Progression par tours

### ✅ Tour 1 — Vulgarisation
- [x] Axe 1 — Godot ✅
- [x] Axe 1 — Unity ✅
- [x] Axe 1 — Unreal ✅
- [x] Axe 1 — Bevy ✅
- [x] Axe 2 — HTML + CSS ✅
- [x] Axe 2 — WPF / XAML ✅
- [x] Axe 2 — Flutter ✅
- [x] Axe 2 — Qt ✅

### ✅ Tour 2 — Intermédiaire
- [x] Axe 1 — Godot ✅
- [x] Axe 1 — Unity ✅
- [x] Axe 1 — Unreal ✅
- [x] Axe 1 — Bevy ✅
- [x] Axe 2 — HTML + CSS ✅
- [x] Axe 2 — WPF / XAML ✅
- [x] Axe 2 — Flutter ✅
- [x] Axe 2 — Qt ✅

### ✅ Tour 3 — Technique
- [x] Axe 1 — Godot ✅
- [x] Axe 1 — Unity ✅
- [x] Axe 1 — Unreal ✅
- [x] Axe 1 — Bevy ✅
- [x] Axe 2 — HTML + CSS ✅
- [x] Axe 2 — WPF / XAML ✅
- [x] Axe 2 — Flutter ✅
- [x] Axe 2 — Qt ✅

### ✅ Synthèse finale
- [x] Tableau comparatif des architectures moteur ✅
- [x] Tableau comparatif des systèmes UI ✅
- [x] Choix d'architecture pour HumbleEngine (argumenté) ✅ → Node Tree (Godot) + Signals (Qt/Godot)
- [x] Choix du système UI pour HumbleEngine (argumenté) ✅ → MVVM + Data Binding (WPF) + Signals (Qt/Godot)

---

## Décisions d'architecture retenues

| Composant | Choix | Inspiré de |
|-----------|-------|-----------|
| Graphe de scène | Node Tree | Godot |
| Communication | Signals & Slots | Qt / Godot |
| Pattern UI | MVVM | WPF |
| Réactivité | Data Binding (INotifyPropertyChanged) | WPF |
| Réutilisabilité | Nodes composites instanciables | Godot / Unity (Prefabs) |
| Ressources / Thèmes | ResourceDictionary | WPF |
| Conteneur global | World | Bevy |

---

## Légende

| Symbole | Signification |
|---------|--------------|
| ⬜ | À faire |
| 🔄 | En cours |
| ✅ | Terminé |
