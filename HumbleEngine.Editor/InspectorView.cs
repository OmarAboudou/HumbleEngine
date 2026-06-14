namespace HumbleEngine;

/// <summary>
/// The inspector panel: shows the editable properties of the currently selected
/// node. It is reactive on <see cref="EditorState.Selection"/> — when the
/// selection changes the panel rebuilds its rows; when it becomes null the panel
/// empties. Each <see cref="InspectorRow"/> is created fresh for the new node,
/// disposing the previous set.
/// <para>
/// The rebuild is a single <see cref="Effect"/> that reads
/// <c>editor.Selection.Value</c> (auto-tracking), clears the column's children,
/// then adds one <see cref="InspectorRow"/> per inspectable property discovered
/// by <see cref="NodeInspector.GetInspectableProperties"/>. No manual
/// <c>.Changed</c> subscription needed — the effect re-runs whenever the
/// selection changes.
/// </para>
/// </summary>
public sealed class InspectorView : UINode
{
    private static readonly Vector4 BgColor  = new(0.11f, 0.11f, 0.14f, 1f);
    private static readonly Vector4 SepColor = new(0.22f, 0.22f, 0.28f, 1f);

    private readonly Panel      _background;
    private readonly Label      _header;
    private readonly Panel      _separator;
    private readonly Column     _rows;
    // Plain list — not a NodeList — so iterating it inside an Effect adds no
    // spurious dependency on the column's structure signal.
    private readonly List<UINode> _activeRows = [];

    /// <summary>
    /// Builds the inspector panel wired to <paramref name="editor"/>'s selection.
    /// </summary>
    public InspectorView(EditorState editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        _background = new Panel { Name = "InspectorBg" };
        _background.Color.Value = BgColor;
        Attach(_background);

        _header = new Label { Hittable = false, Name = "InspectorHeader" };
        _header.Color.Value = new Vector4(0.9f, 0.9f, 0.9f, 1f);
        Attach(_header);

        _separator = new Panel { Name = "InspectorSep" };
        _separator.Color.Value = SepColor;
        Attach(_separator);

        _rows = new Column { Name = "InspectorRows" };
        _rows.Spacing.Value = 2f;
        Attach(_rows);

        // Rebuild the rows every time the selection changes — and only then.
        // Reading Selection.Value auto-tracks (the one intended dependency); the
        // rebuild itself runs untracked, because building the rows reads the source
        // values they bind to (e.g. a TextField's initial text). Without Untrack
        // those reads would subscribe this effect, so editing a value would rebuild
        // the rows mid-keystroke and steal the focus from the field being typed in.
        CreateEffect(() =>
        {
            var selected = editor.Selection.Value;
            Reactive.Untrack(() => Rebuild(selected));
        });

        // Sync background + children sizes when this panel's own Size changes.
        CreateEffect(SyncLayout);
    }

    // ── Rebuild ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Clears the current rows and populates with one <see cref="InspectorRow"/>
    /// per inspectable property of <paramref name="node"/>. When <paramref name="node"/>
    /// is null the panel shows a placeholder message and stays empty.
    /// </summary>
    private void Rebuild(Node? node)
    {
        // Dispose (not just detach) the previous rows: each Dispose() releases the
        // row's Effects and C# event subscriptions, then detaches the row from _rows.
        foreach (var row in _activeRows)
            row.Dispose();
        _activeRows.Clear();

        if (node is null)
        {
            _header.Text.Value = "Inspector";
            return;
        }

        _header.Text.Value = node.Name is { Length: > 0 } name
            ? $"{node.GetType().Name}  '{name}'"
            : node.GetType().Name;

        var props = NodeInspector.GetInspectableProperties(node);
        foreach (var prop in props)
        {
            var row = new InspectorRow(prop);
            _rows.Children.Add(row);
            _activeRows.Add(row);
        }
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    private const float HeaderHeight = 22f;
    private const float SepHeight    = 1f;
    private const float Inset        = 4f;

    /// <summary>
    /// Tiles the background, header and separator to fill <see cref="UINode.Size"/>,
    /// and positions the rows column (which measures itself from its rows — no size
    /// is imposed). Runs as an <see cref="Effect"/> so it adjusts when the editor
    /// layout resizes the panel.
    /// </summary>
    private void SyncLayout()
    {
        var sz = Size.Value;

        _background.Position.Value = Vector2.Zero;
        _background.Size.Value     = sz;

        _header.Position.Value = new Vector2(Inset, (HeaderHeight - _header.Size.Value.Y) / 2f);

        _separator.Position.Value = new Vector2(0f, HeaderHeight);
        _separator.Size.Value     = new Vector2(sz.X, SepHeight);

        // The rows Column content-sizes itself (the layout protocol); just place it.
        _rows.Position.Value = new Vector2(Inset, HeaderHeight + SepHeight + Inset);
    }
}
