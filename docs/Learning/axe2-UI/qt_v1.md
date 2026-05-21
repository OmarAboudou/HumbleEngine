# Qt — Fiche de révision

> Axe 2 — Systèmes UI & frameworks applicatifs
> Dernière mise à jour : Tour 1 (Vulgarisation) ✅

---

## L'idée centrale

Qt est un framework UI né en 1995, écrit en C++, conçu pour créer des applications de bureau **cross-platform** — une seule base de code qui tourne sur Windows, macOS et Linux. Son système de **Signals & Slots** a été extrêmement influent et a directement inspiré les Signaux de Godot.

---

## Les concepts fondamentaux

| Concept | Définition courte |
|---------|------------------|
| **Widget** | La brique de base de l'interface — bouton, champ texte, fenêtre… |
| **Signal** | Un événement émis par un objet : "quelque chose s'est passé". |
| **Slot** | Une fonction qui réagit à un Signal. |
| **Connexion** | Le lien explicite entre un Signal et un Slot via `connect()`. |
| **Layout** | Conteneur de mise en page qui organise les Widgets. |

---

## Signals & Slots — L'idée forte

C'est le cœur de Qt. Un objet émet un **Signal** quand quelque chose se passe. Un autre objet expose un **Slot** — une fonction qui peut réagir. On **connecte** explicitement les deux.

```cpp
connect(bouton, &QPushButton::clicked,
        this,   &MaFenetre::sauvegarder);
```

L'émetteur ne sait pas qui écoute. L'écouteur ne sait pas d'où vient le Signal. **Communication découplée** — exactement comme les Signaux de Godot, qui s'en sont directement inspirés.

Un Signal peut être connecté à plusieurs Slots. Un Slot peut recevoir plusieurs Signals.

---

## Les Widgets Qt

```
QMainWindow
└── QWidget (zone centrale)
    └── QVBoxLayout
        ├── QLabel      "Bonjour"
        ├── QLineEdit   [champ texte]
        └── QPushButton "Valider"
```

---

## Les Layouts — Mise en page

| Layout | Comportement |
|--------|-------------|
| `QVBoxLayout` | Empile ses enfants verticalement |
| `QHBoxLayout` | Aligne ses enfants horizontalement |
| `QGridLayout` | Organise en grille lignes/colonnes |

---

## Comparaison des systèmes UI

```
HTML/CSS   → structure/apparence séparées, sélecteurs CSS
WPF/XAML   → XAML + Data Binding C#
Flutter    → tout est Widget, rendu pixel par pixel
Qt         → Widgets C++ + Signals & Slots, cross-platform natif
```

---

## À retenir absolument

1. **Widget** = brique de base de l'interface Qt.
2. **Signal** = événement émis par un objet ("quelque chose s'est passé").
3. **Slot** = fonction qui réagit à un Signal.
4. **connect()** = le lien explicite entre un Signal et un Slot.
5. **Cross-platform natif** = une base de code, tous les OS desktop.
6. Les Signaux de **Godot sont directement inspirés** du système Qt.

---

## Quiz — Questions clés

- Qu'est-ce qu'un Signal dans Qt ?
- Qu'est-ce qu'un Slot ?
- Comment établit-on le lien entre un Signal et un Slot ?
- Quel framework de moteur de jeu s'est inspiré du système de Qt ?
- Quelle est la grande promesse de Qt par rapport aux autres systèmes UI ?

---

*Tour 2 (intermédiaire) : QObject et le meta-object system, Signals & Slots avancés, Qt Quick / QML, modèle/vue (MVC), gestion des événements.*
