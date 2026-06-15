namespace HumbleEngine;

/// <summary>
/// The editor shell: a self-contained component that owns an <see cref="EditorState"/>,
/// provides it to its entire subtree (hierarchy, inspector and any future panel all
/// inherit it without constructor threading), and hosts the three-pane layout
/// (hierarchy · inspector · viewport). Drop one <see cref="Editor"/> as the
/// <see cref="SceneTree.Root"/> — it self-seeds from the surface size.
/// </summary>
public sealed class Editor : Scene
{
    private const float HierarchyW = 220f;
    private const float InspectorW = 300f;

    private readonly Row _shell;
    private bool _initialized;

    /// <summary>
    /// Builds the editor for <paramref name="target"/>, the scene being inspected
    /// and rendered in the viewport.
    /// </summary>
    public Editor(Node target)
    {
        ArgumentNullException.ThrowIfNull(target);

        // Publish EditorState for the whole subtree — panels inherit it in OnAttached.
        Provide<EditorState>(new EditorState());

        _shell = new Row { Name = "EditorShell" };
        _shell.Spacing.Value = 8f;

        var hierarchy = new HierarchyView(target) { Name = "Hierarchy" };
        hierarchy.Size.Value = new Vector2(HierarchyW, 0f);

        var inspector = new InspectorView { Name = "Inspector" };
        inspector.Size.Value = new Vector2(InspectorW, 0f);

        var viewport = new ViewportNode { Name = "Viewport" };
        viewport.Content = target;

        _shell.Add(hierarchy);
        _shell.Add(inspector);
        _shell.Add(new Expanded(viewport));
        Attach(_shell);
    }

    /// <inheritdoc />
    protected override void OnAttached()
    {
        if (_initialized) return;
        _initialized = true;
        // Forward the surface size to the inner shell so layout works when Editor
        // is the SceneTree root (SceneTree only seeds UINode roots directly).
        CreateEffect(() =>
        {
            var size = Tree!.SurfaceSize.Value;
            if (size.X > 0f && size.Y > 0f)
                _shell.Incoming.Value = Constraints.Tight(size);
        });
    }
}
