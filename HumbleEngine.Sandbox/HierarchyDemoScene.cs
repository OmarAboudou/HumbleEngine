namespace HumbleEngine.Sandbox;

/// <summary>
/// Editor demo (roadmap 13, blocs 2-4): a small "scene being edited" introspected
/// and rendered live. Three panels:
/// <list type="bullet">
///   <item><b>Hierarchy</b> (left) — live tree view; click to select.</item>
///   <item><b>Inspector</b> (middle) — properties of the selected node, editable.</item>
///   <item><b>Viewport</b> (right) — live render of the edited scene: the sample tree
///     is the <see cref="ViewportNode.Content"/>, so it enters the living tree and its
///     nodes draw with positions naturally offset by the viewport's own position.</item>
/// </list>
/// </summary>
public sealed class HierarchyDemoScene : Scene
{
    private const float PanelY      = 20f;
    private const float PanelHeight = 500f;
    private const float HierarchyW  = 220f;
    private const float InspectorW  = 300f;
    private const float ViewportW   = 380f;
    private const float Gap         = 16f;
    private const float MarginLeft  = 20f;

    public HierarchyDemoScene()
    {
        var editor = new EditorState();
        var sample = BuildSampleScene();

        var hierarchy = new HierarchyView(sample, editor) { Name = "Hierarchy" };
        hierarchy.Position.Value = new Vector2(MarginLeft, PanelY);
        hierarchy.Size.Value     = new Vector2(HierarchyW, PanelHeight);
        Attach(hierarchy);

        var inspector = new InspectorView(editor) { Name = "Inspector" };
        inspector.Position.Value = new Vector2(MarginLeft + HierarchyW + Gap, PanelY);
        inspector.Size.Value     = new Vector2(InspectorW, PanelHeight);
        Attach(inspector);

        var viewport = new ViewportNode { Name = "Viewport" };
        viewport.Position.Value = new Vector2(MarginLeft + HierarchyW + Gap + InspectorW + Gap, PanelY);
        viewport.Size.Value     = new Vector2(ViewportW, PanelHeight);
        viewport.Content        = sample;   // adopts the sample into the living tree
        Attach(viewport);
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
