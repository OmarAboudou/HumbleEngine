namespace HumbleEngine.Sandbox;

/// <summary>
/// Root scene of the Sandbox: composes the triangle node (roadmap 07) and a
/// translucent panel drawn over it (roadmap 08 — attach order is painter's
/// order), and narrates its tree lifecycle on the console. Default-constructible
/// like every node (roadmap 09): GPU resources come from the tree the scene
/// enters, not from constructor arguments.
/// </summary>
public sealed class SandboxScene : Scene
{
    private TriangleNode? _triangle;
    private readonly MovablePanel _panel;
    private readonly Panel _breathing;

    /// <summary>
    /// Size of the column's middle panel — the Sandbox animates its height to
    /// show the reactive layout: the panel below slides on its own.
    /// </summary>
    public Reactive<Vector2> BreathingSize => _breathing.Size;

    /// <summary>
    /// Builds the interior: a triangle, a panel over its heart, and a column
    /// of three panels on the right (roadmap 08, Bloc 4).
    /// </summary>
    public SandboxScene()
    {
        _triangle = new TriangleNode { Name = "Triangle" };
        Attach(_triangle);

        _panel = new MovablePanel { Name = "Panel" };
        _panel.Position.Value = new Vector2(250f, 150f);
        _panel.Size.Value     = new Vector2(300f, 200f);
        _panel.Color.Value    = new Vector4(0.2f, 0.5f, 0.9f, 0.7f);
        Attach(_panel);

        var image = new ImageNode { Name = "Image" };
        image.Position.Value = new Vector2(60f, 360f);
        image.Size.Value     = new Vector2(180f, 180f);
        Attach(image);

        _breathing = MakeTile(new Vector4(0.3f, 0.8f, 0.4f, 1f));
        var column = new Column { Name = "Column" };
        column.Position.Value = new Vector2(620f, 40f);
        column.Spacing.Value  = 10f;
        column.Children.Add(MakeTile(new Vector4(0.9f, 0.35f, 0.3f, 1f)));
        column.Children.Add(_breathing);
        column.Children.Add(MakeTile(new Vector4(0.95f, 0.8f, 0.3f, 1f)));
        Attach(column);
    }

    /// <summary>A 150×60 interactive tile for the column (hover + click-to-dispose).</summary>
    private static Tile MakeTile(Vector4 color)
    {
        var tile = new Tile(color);
        tile.Size.Value = new Vector2(150f, 60f);
        return tile;
    }

    /// <summary>
    /// Queues the triangle's disposal, honoured at the end-of-frame flush — the
    /// Bloc 3 proof that the tree drives the drawing: the node dies, the
    /// triangle vanishes. No-op once the triangle is gone.
    /// </summary>
    public void DisposeTriangle()
    {
        if (_triangle is null)
            return;
        Console.WriteLine($"[SceneTree] {_triangle} queued for disposal — the triangle vanishes at end of frame.");
        _triangle.QueueDispose();
        _triangle = null;
    }

    /// <inheritdoc />
    protected override void OnAttached() =>
        Console.WriteLine($"[SceneTree] {this} attached — the tree is alive.");

    /// <inheritdoc />
    protected override void OnDetached() =>
        Console.WriteLine($"[SceneTree] {this} detached.");

    /// <inheritdoc />
    protected override void OnDispose() =>
        Console.WriteLine($"[SceneTree] {this} disposed.");
}
