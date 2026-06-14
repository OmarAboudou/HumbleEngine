namespace HumbleEngine.Sandbox;

/// <summary>
/// Editor demo (roadmaps 13-14): a small "scene being edited" introspected and
/// rendered live, in a shell that <b>reflows with the window</b>. The shell is a
/// <see cref="Row"/> (the demo's root, so the tree seeds it from the surface size):
/// <list type="bullet">
///   <item><b>Hierarchy</b> (left, fixed width) — live tree view; click to select.</item>
///   <item><b>Inspector</b> (middle, fixed width) — properties of the selected node,
///     editable; parent-data (flex Factor/Tight) shows for nodes under a container.</item>
///   <item><b>Viewport</b> (right, <see cref="Expanded"/>) — takes the remaining width;
///     renders the sample tree (its <see cref="ViewportNode.Content"/>). Resizing the
///     window redistributes the free width and fills the height.</item>
/// </list>
/// </summary>
public static class HierarchyDemoScene
{
    private const float HierarchyW = 220f;
    private const float InspectorW = 300f;

    /// <summary>Builds the editor shell (a reflowing <see cref="Row"/>) — the demo's root.</summary>
    public static Row Build()
    {
        var shell = new Row { Name = "HierarchyDemo" };
        shell.Spacing.Value = 8f;

        var editor = new EditorState();
        var sample = BuildSampleScene();

        var hierarchy = new HierarchyView(sample, editor) { Name = "Hierarchy" };
        hierarchy.Size.Value = new Vector2(HierarchyW, 0f);   // requested width; height filled

        var inspector = new InspectorView(editor) { Name = "Inspector" };
        inspector.Size.Value = new Vector2(InspectorW, 0f);

        var viewport = new ViewportNode { Name = "Viewport" };
        viewport.Content = sample;   // adopts the sample into the living tree

        shell.Add(hierarchy);
        shell.Add(inspector);
        shell.Add(new Expanded(viewport));  // takes the remaining width
        return shell;
    }

    /// <summary>
    /// A small named UI tree that acts as the "scene being edited". It starts
    /// detached, then is adopted by the <see cref="ViewportNode"/> — Labels
    /// measure on <c>OnAttached</c>, the Column stacks reactively.
    /// </summary>
    private static Node BuildSampleScene()
    {
        var header = new Panel { Name = "Header" };
        header.Color.Value = new Vector4(0.15f, 0.25f, 0.45f, 1f);
        header.Size.Value  = new Vector2(340f, 30f);

        var title = new Label { Name = "Title" };
        title.Text.Value = "Hello, Viewport!";

        var image = new Panel { Name = "Image" };
        image.Color.Value = new Vector4(0.2f, 0.5f, 0.8f, 1f);
        image.Size.Value  = new Vector2(120f, 80f);

        var field = new TextField { Name = "Field" };
        field.Text.Value = "editable in viewport";
        field.Size.Value = new Vector2(200f, 24f);

        var content = new Column { Name = "Content" };
        content.Spacing.Value = 6f;
        content.Children.Add(title);
        content.Children.Add(image);
        content.Children.Add(field);

        var sidebar = new Panel { Name = "Sidebar" };
        sidebar.Color.Value = new Vector4(0.10f, 0.10f, 0.16f, 1f);
        sidebar.Size.Value  = new Vector2(80f, 150f);

        var body = new Row { Name = "Body" };
        body.Spacing.Value = 8f;
        body.Children.Add(sidebar);
        body.Children.Add(content);

        var root = new Column { Name = "Root" };
        root.Spacing.Value = 4f;
        root.Children.Add(header);
        root.Children.Add(body);
        return root;
    }
}
