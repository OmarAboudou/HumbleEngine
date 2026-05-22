# Phase 3 — Input system, HitTest, Button ✅

---

## `HitTestFilter` — `Scene/HitTestFilter.cs`

```csharp
public enum HitTestFilter { Ignore, Pass, Stop, Disabled }
//  Ignore   = hover uniquement, pas de click (défaut Node)
//  Pass     = hover + click, propagé vers les ancêtres
//  Stop     = hover + click, stoppé
//  Disabled = aucun événement (ni hover ni click)
```

---

## `HitTest` — `Scene/HitTest.cs`

```csharp
public static class HitTest
{
    public static Node? Find(Node root, float x, float y)          // depth-first, filtre ignoré
    public static void DispatchClick(Node? node)                    // remonte avec filtre
    public static List<Node> GetHoveredPath(Node? node)            // exclut Disabled
    public static Node? FirstInteractive(List<Node> path)          // premier Pass ou Stop
}
```

**Règles :**
- Clic confirmé : `_pressedNode` (FirstInteractive au MouseDown) encore dans le chemin au MouseUp
- `MouseEnter`/`Leave` : diff ancien/nouveau chemin, toujours propagés (pas filtrés)
- `MouseFilter` n'affecte que les clics, pas le hover

---

## Input dans `Application`

```
mouse.MouseMove  → HitTest.Find → diff _hoveredPath → OnMouseLeave/OnMouseEnter
mouse.MouseDown  → _pressedNode = FirstInteractive(_hoveredPath) + OnMouseDown
mouse.MouseUp    → OnMouseUp + si _pressedNode ∈ _hoveredPath → DispatchClick
```

---

## `Button` — `Nodes/Button.cs`

```csharp
public class Button : Node
{
    public ReactiveProperty<string>  Text            = new("");
    public ReactiveProperty<SKColor> BackgroundColor = new(SKColor(220,220,220));
    public ReactiveProperty<SKColor> HoverColor      = new(SKColor(190,210,240));
    public ReactiveProperty<float>   FontSize        = new(16f);
    public Signal Pressed { get; }
    public override HitTestFilter MouseFilter => HitTestFilter.Stop;
    // _isHovered toggle → MarkPaintDirty
    // RenderContent : new Box { new Span(...) }.Color(bg).Padding(16f, 8f)
}
```
