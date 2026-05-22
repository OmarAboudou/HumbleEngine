# Phase 5 — TextInput + premier écran MVVM ✅

Tests : 82/82 ✅

---

## Infrastructure clavier + focus

`Application` : `_focusedNode`, `SetFocus()`, dispatch `OnKeyChar`/`OnKeyDown` vers le nœud focalisé.
Focus déclenché automatiquement dans `OnMousePressed` si `node.IsFocusable == true`.

```csharp
// Node
public virtual bool IsFocusable => false;
public virtual void OnFocusGained() { }
public virtual void OnFocusLost()   { }
public virtual void OnKeyChar(char c)  { }
public virtual void OnKeyDown(Key key) { }
```

---

## TextInput

```csharp
public class TextInput : Node
{
    public ReactiveProperty<string> Text        = new("");
    public ReactiveProperty<string> Placeholder = new("...");
    public ReactiveProperty<float>  FontSize    = new(16f);

    public override HitTestFilter MouseFilter => HitTestFilter.Stop;
    public override bool IsFocusable => true;

    public override void OnKeyChar(char c)   => Text.Value += c;
    public override void OnKeyDown(Key key)
    {
        if (key == Key.Backspace && Text.Value.Length > 0)
            Text.Value = Text.Value[..^1];
    }
    // RenderContent : Box [ Span ] avec bordure bleue si focalisé
}
```

---

## Box.Border

`BoxData` : `BorderColor` + `BorderWidth`.
`Box.Border(SKColor color, float width = 1f)`.
`RenderTree.PaintBox` : second draw call `SKPaintStyle.Stroke` si `BorderWidth > 0`.

---

## Premier écran MVVM

**Pattern :** ViewModel = plain C# + `ReactiveProperty<T>`. Node = View. Zéro dépendance croisée.

```csharp
// Two-way binding
input.Text.BindFrom(vm.Username);
vm.Username.BindFrom(input.Text);

// One-way binding
greetingLabel.Text.BindFrom(vm.Greeting);
```

**Chaîne réactive validée :** `OnKeyChar` → `TextInput.Text` → `Vm.Username` → `Vm.Greeting` → `Label.Text`

---

## HitTest sub-node (future)

Noté dans `docs/future-ideas.md` : HitTest au niveau `RenderElement` pour les zones interactives sub-node (style Flutter `RenderBox.hitTest`).
