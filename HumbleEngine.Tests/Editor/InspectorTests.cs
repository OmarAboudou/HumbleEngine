namespace HumbleEngine.Tests.Editor;

/// <summary>
/// Unit tests for <see cref="NodeInspector"/> and <see cref="InspectorView"/>:
/// property discovery by reflection, row creation on selection, rebuild on
/// selection change, and disposal of stale rows.
/// </summary>
public sealed class InspectorTests
{
    // ── NodeInspector.GetInspectableProperties ────────────────────────────────

    [Test]
    public void GetInspectableProperties_FindsUiNodeProperties()
    {
        var node = new Panel();

        var props = NodeInspector.GetInspectableProperties(node);
        var names = props.Select(p => p.Name).ToList();

        // UINode exposes Position and Size; Panel adds Color.
        Assert.That(names, Does.Contain("Position"));
        Assert.That(names, Does.Contain("Size"));
        Assert.That(names, Does.Contain("Color"));
    }

    [Test]
    public void GetInspectableProperties_BasePropertiesBeforeDerived()
    {
        var node = new Panel();

        var props  = NodeInspector.GetInspectableProperties(node);
        var names  = props.Select(p => p.Name).ToList();
        var posIdx = names.IndexOf("Position");
        var colIdx = names.IndexOf("Color");

        // UINode (base) declares Position; Panel (derived) declares Color.
        Assert.That(posIdx, Is.LessThan(colIdx));
    }

    [Test]
    public void GetInspectableProperties_ReturnsLiveValues()
    {
        var node = new Panel();
        node.Color.Value = new Vector4(1f, 0f, 0f, 1f);

        var props = NodeInspector.GetInspectableProperties(node);
        var color = props.Single(p => p.Name == "Color");

        Assert.That(color.Value, Is.InstanceOf<IObservableValue<Vector4>>());
        Assert.That(((IObservableValue<Vector4>)color.Value).Value,
            Is.EqualTo(new Vector4(1f, 0f, 0f, 1f)));
    }

    [Test]
    public void GetInspectableProperties_ExcludesNonObservableMembers()
    {
        var node = new Panel();

        var props = NodeInspector.GetInspectableProperties(node);
        var names = props.Select(p => p.Name).ToList();

        // string? Name, bool Hittable, Node? Parent, etc. must not appear.
        Assert.That(names, Does.Not.Contain("Name"));
        Assert.That(names, Does.Not.Contain("Hittable"));
        Assert.That(names, Does.Not.Contain("Parent"));
        Assert.That(names, Does.Not.Contain("Children"));
    }

    [Test]
    public void GetInspectableProperties_FindsComputed()
    {
        var node = new Panel();

        var names = NodeInspector.GetInspectableProperties(node).Select(p => p.Name).ToList();

        // Area is a Computed<float> — discovered like any IObservableValue.
        Assert.That(names, Does.Contain("Area"));
    }

    [Test]
    public void InspectorRow_ForComputed_IsReadOnlyLabelAndLive()
    {
        var node = new Panel();
        node.Size.Value = new Vector2(3f, 4f);          // Area = 12
        var area = NodeInspector.GetInspectableProperties(node).Single(p => p.Name == "Area");

        var row    = new InspectorRow(area);
        var widget = row.Children.Single(c => c.Name != "PropName");

        // A Computed is read-only → a Label, never an editable TextField.
        Assert.That(widget, Is.InstanceOf<Label>());
        Assert.That(widget, Is.Not.InstanceOf<TextField>());

        // And live: changing Size recomputes Area, the label follows.
        var label = (Label)widget;
        Assert.That(label.Text.Value, Does.Contain("12"));
        node.Size.Value = new Vector2(5f, 4f);          // Area = 20
        Assert.That(label.Text.Value, Does.Contain("20"));
    }

    // ── InspectorView rows ────────────────────────────────────────────────────

    private static Column RowsColumn(InspectorView view) =>
        view.Children.OfType<Column>().Single();

    [Test]
    public void InspectorView_StartsEmpty_WhenNoSelection()
    {
        var editor  = new EditorState();
        var inspector = new InspectorView(editor);

        Assert.That(RowsColumn(inspector).Children.Count, Is.EqualTo(0));
    }

    [Test]
    public void InspectorView_BuildsOneRowPerInspectableProperty()
    {
        var editor    = new EditorState();
        var inspector = new InspectorView(editor);
        var node      = new Panel();

        editor.Selection.Value = node;

        var expected = NodeInspector.GetInspectableProperties(node).Count;
        Assert.That(RowsColumn(inspector).Children.Count, Is.EqualTo(expected));
    }

    [Test]
    public void InspectorView_ClearsRows_WhenSelectionBecomesNull()
    {
        var editor    = new EditorState();
        var inspector = new InspectorView(editor);
        var node      = new Panel();

        editor.Selection.Value = node;
        editor.Selection.Value = null;

        Assert.That(RowsColumn(inspector).Children.Count, Is.EqualTo(0));
    }

    [Test]
    public void InspectorView_RebuildsRows_OnSelectionChange()
    {
        var editor    = new EditorState();
        var inspector = new InspectorView(editor);
        var panel     = new Panel();
        var label     = new Label();

        editor.Selection.Value = panel;
        var rowsForPanel = RowsColumn(inspector).Children.Count;

        editor.Selection.Value = label;
        var rowsForLabel = RowsColumn(inspector).Children.Count;

        // Both have rows, and the counts reflect their distinct properties.
        Assert.That(rowsForPanel, Is.GreaterThan(0));
        Assert.That(rowsForLabel, Is.GreaterThan(0));
        // Label has Text in addition to Position+Size; Panel has Color instead.
        Assert.That(rowsForPanel, Is.Not.EqualTo(rowsForLabel));
    }

    [Test]
    public void InspectorView_DisposesPreviousRows_OnRebuild()
    {
        var editor    = new EditorState();
        var inspector = new InspectorView(editor);
        var nodeA     = new Panel();
        var nodeB     = new Panel();

        editor.Selection.Value = nodeA;
        var oldRow = RowsColumn(inspector).Children[0];

        editor.Selection.Value = nodeB;

        // The old row must be disposed — no longer usable.
        Assert.That(oldRow.IsDisposed, Is.True);
    }

    [Test]
    public void InspectorView_DoesNotRebuild_WhenAnEditedValueChanges()
    {
        // Regression: editing a value (what a widget writes back) must not rebuild
        // the rows. The rebuild effect depends only on the selection — not on the
        // source values the rows read while binding. Otherwise the field being
        // typed in is disposed mid-keystroke and the focus is lost.
        var editor    = new EditorState();
        var inspector = new InspectorView(editor);
        var column    = new Column();   // exposes a writable Property<float> Spacing

        editor.Selection.Value = column;
        var rowsBefore = RowsColumn(inspector).Children.ToArray();

        // Simulate the user editing a value through the inspector.
        column.Spacing.Value = 42f;

        var rowsAfter = RowsColumn(inspector).Children.ToArray();
        Assert.That(rowsAfter, Is.EqualTo(rowsBefore));           // same instances, not recreated
        Assert.That(rowsBefore.All(r => !r.IsDisposed), Is.True); // none disposed
    }
}
