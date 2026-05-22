# Phase 4 — Layout pivot + Column/Row ✅

Tests : 72/72 ✅

---

## Layout pivot — RenderTree owns layout

**Avant :** `Node.Layout(Size available)` calculait `ComputedBounds`.
**Après :** `RenderTree.Layout(BoxConstraints)` calcule tout, écrit `Node.ComputedBounds`.

**Algorithme :**
- Descente : `BoxConstraints` parent → enfant
- Remontée : tailles enfant → parent (hug content ou taille explicite)
- `Span` → taille intrinsèque via `SKFont.MeasureText`
- `Box` → padding + max enfant (layering)
- `VLayout` → padding + somme enfants + spacing (vertical)
- `HLayout` → padding + somme enfants + spacing (horizontal)

---

## Nouveaux types de layout

```csharp
public readonly struct BoxConstraints
{
    public static BoxConstraints Loose(Size available)  // min=0, max=available
    public static BoxConstraints Tight(Size size)        // min=max=size
    public static BoxConstraints Unconstrained           // max=∞
    public Size Constrain(float width, float height)     // clamp
}

public readonly struct LayoutData
{
    public float? Width, Height;  // null = hug content
    public float  PaddingX, PaddingY;
}

public readonly struct LinearLayoutData { public float Spacing; }
// Partagé entre VLayout et HLayout
```

---

## Render elements (IRenderElement / ICompositeRenderElement)

**Convention :** `XXXLayout` = pas de visuel propre ; sans suffix = visuel.

```csharp
// VLayout / HLayout — layout pur, [CollectionBuilder] supporté
VLayout layout = [child1, ..spread, child2];
return layout.Spacing(8f).Padding(16f);

// Box — fond + enfants
Box box = [child];
return box.Color(SKColors.Red).CornerRadius(4f);

// Span — texte stylé
new Span("text").Color(SKColors.Gray).FontSize(12f).Width(200f)
```

**`RenderElementExtensions`** — sur tout `T : struct, IRenderElement` :
`Width`, `Height`, `Padding` + `Create<T>` (helper pour `[CollectionBuilder]`)

---

## Column / Row Nodes — `Nodes/Layout/`

```csharp
public class Column : Node
{
    public ReactiveProperty<float> Spacing = new(0f);
    protected override RenderDescription RenderContent()
    {
        VLayout layout = [..Children.Select(c => c.Render())];
        return layout.Spacing(Spacing.Value);
    }
}
// Row : identique avec HLayout
```

---

## Nomenclature Rendering (refactor)

| Ancien | Actuel |
|--------|--------|
| `RenderNodeTree` | `RenderTree` |
| `RenderNode` struct | `RenderEntry` |
| `RenderNodeKind` | `RenderEntryKind` |
| `IRenderNode` | `IRenderElement` |
| `ICompositeRenderNode` | `ICompositeRenderElement` |
| `RenderNodeExtensions` | `RenderElementExtensions` |
| `Builders/` | `RenderElements/` |
