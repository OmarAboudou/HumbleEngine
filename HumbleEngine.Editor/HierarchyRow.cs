namespace HumbleEngine;

/// <summary>
/// One line of the hierarchy panel: a node's name above its children, indented and
/// recursive. The children mirror <see cref="HumbleEngine.Node.Children"/> through
/// <c>BindItemsFrom</c>, so the row follows the real tree. Clicking the row selects
/// its node in the <see cref="EditorState"/>; a highlight follows the selection.
/// <para>
/// The label is <see cref="UINode.Hittable"/> = false (decorative): the click falls
/// through it to the row, which handles selection — text isn't selectable here, like
/// a Godot button. The row lays itself out and computes its own
/// <see cref="UINode.Size"/> (containers don't self-measure on this étage), giving
/// the size cascade for free.
/// </para>
/// </summary>
public sealed class HierarchyRow : UINode
{
    private const float Indent = 16f;
    private static readonly Vector4 Highlight = new(0.25f, 0.45f, 0.85f, 1f);
    private static readonly Vector4 Transparent = new(0f, 0f, 0f, 0f);

    private readonly EditorState _editor;
    private readonly Panel _background;
    private readonly Label _label;
    private readonly NodeList<HierarchyRow> _subRows;

    /// <summary>Creates the row for <paramref name="node"/> and its subtree.</summary>
    public HierarchyRow(Node node, EditorState editor)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(editor);
        Node = node;
        _editor = editor;

        _background = new Panel();
        Attach(_background);

        _label = new Label { Hittable = false }; // decorative — the click reaches the row
        _label.Text.Value = node.Name ?? node.GetType().Name;
        Attach(_label);

        _subRows = CreateChildList<HierarchyRow>();
        _subRows.BindItemsFrom(node.Children, child => new HierarchyRow(child, editor));

        // Highlight the background while this row's node is the selection.
        CreateEffect(() =>
            _background.Color.Value =
                ReferenceEquals(_editor.Selection.Value, Node) ? Highlight : Transparent);

        CreateEffect(Layout);
    }

    /// <summary>The scene node this row represents.</summary>
    public Node Node { get; }

    /// <inheritdoc />
    protected override bool OnInput(InputEvent inputEvent)
    {
        if (inputEvent is PointerPressed { Button: PointerButton.Left })
        {
            _editor.Selection.Value = Node;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Stacks the label then the indented sub-rows, and reports the total size. Runs
    /// as an <see cref="Effect"/>: it reads the label and sub-row sizes and the
    /// sub-rows list, re-running on any change; it writes positions and its own size,
    /// neither of which it reads — no feedback loop.
    /// </summary>
    private void Layout()
    {
        var labelSize = _label.Size.Value;
        _label.Position.Value = Vector2.Zero;
        _background.Position.Value = Vector2.Zero;
        _background.Size.Value = labelSize;
        var y = labelSize.Y;
        var width = labelSize.X;
        for (var i = 0; i < _subRows.Count; i++)
        {
            var row = _subRows[i];
            row.Position.Value = new Vector2(Indent, y);
            y += row.Size.Value.Y;
            width = MathF.Max(width, Indent + row.Size.Value.X);
        }
        Size.Value = new Vector2(width, y);
    }
}
