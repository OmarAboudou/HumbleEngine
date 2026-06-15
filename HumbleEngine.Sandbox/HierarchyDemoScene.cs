namespace HumbleEngine.Sandbox;

/// <summary>
/// Editor demo (roadmaps 13-15): a small "scene being edited" introspected and
/// rendered live. The root is an <see cref="Editor"/> — it owns the
/// <see cref="EditorState"/>, provides it to the whole subtree, and hosts the
/// three-pane layout (hierarchy · inspector · viewport) that reflows with the window.
/// </summary>
public static class HierarchyDemoScene
{
    /// <summary>Builds the editor root — an <see cref="Editor"/> wrapping a sample scene.</summary>
    public static Editor Build() => new(BuildSampleScene());

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
