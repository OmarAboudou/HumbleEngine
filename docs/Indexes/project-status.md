# HumbleEngine — Index projet

> Fichier auto-maintenu par Claude. Ne pas éditer manuellement.
> Chargé en début de chaque session.

---

## Structure du projet

```
HumbleEngine/
├── Application.cs
├── Reactivity/          Signal, ReactiveProperty, ReactiveCollection
├── Scene/               Node, DirtyLevel, Geometry, HitTestFilter, HitTest
├── Rendering/           RenderEntry, RenderTree, RenderDescription, RenderEntryKind
│   │                    BoxConstraints, LayoutData, LinearLayoutData
│   │                    IRenderElement, ICompositeRenderElement, RenderElementExtensions
│   └── RenderElements/
│       ├── Span/        Span, SpanData
│       ├── Box/         Box, BoxData (BackgroundColor, CornerRadius, BorderColor, BorderWidth)
│       ├── VLayout/     VLayout (vertical, XXXLayout = pas de visuel)
│       └── HLayout/     HLayout (horizontal)
└── Nodes/
    ├── Label.cs
    ├── Button.cs
    ├── TextInput.cs
    └── Layout/          Column, Row

HumbleEngine.Tests/
├── Core/                NodeTests, SignalTests, ReactivePropertyTests, ReactiveCollectionTests
│                        TextInputTests
├── Rendering/           BoxConstraintsTests, LayoutTests
├── Input/               HitTestTests
└── Mvvm/                LoginViewModel, LoginScreen, MvvmTests
```

Tous les types dans `namespace HumbleEngine;`

---

## Phases

| Phase | Contenu | Statut | Détails |
|-------|---------|--------|---------|
| 1 + 2 | Primitives + Premier rendu | ✅ | `docs/Status/phase-1-2.md` |
| 3 | Input system, HitTest, Button | ✅ | `docs/Status/phase-3.md` |
| 4 | Layout pivot, Column/Row, nomenclature | ✅ | `docs/Status/phase-4.md` |
| 5 | TextInput, premier écran MVVM | ✅ | `docs/Status/phase-5.md` |

Tests : **82/82** ✅

---

## Règles invariantes

**Code**
- `namespace HumbleEngine;` — partout, pas de sous-namespaces
- Framework de tests : **NUnit** (pas xUnit)
- Layout dans `RenderTree`, **jamais** dans `Node`
- `Node.Render()` non-virtual → `RenderContent()` surchargé (Template Method)
- `[CollectionBuilder]` sur tout `ICompositeRenderElement` — syntaxe `[..spread]`
- Convention nommage : `XXXLayout` = pas de visuel propre ; sans suffix = visuel
- `ComputedBounds` — écrit par `RenderTree` après layout pass, lu par HitTest

**Process**
- Une étape à la fois, récap + quiz après chaque étape
- Commit + push après chaque étape validée
- Merger via PR sur `develop`, nouvelle branche pour chaque phase
- Mettre à jour `docs/Indexes/project-status.md` après chaque phase
