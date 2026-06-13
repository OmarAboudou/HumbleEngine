namespace HumbleEngine;

/// <summary>
/// The first concrete UI node: fills its rectangle with a colour — übershader
/// mode 0, alpha blended. The bread and butter of every UI: backgrounds,
/// separators, and the base of interactive surfaces (open for inheritance
/// since the Sandbox tile — buttons will follow).
/// </summary>
public class Panel : UINode
{
    /// <summary>RGBA colour of the filled rectangle. Opaque white by default.</summary>
    public Property<Vector4> Color { get; }

    public Panel()
    {
        Color = CreateProperty(new Vector4(1f, 1f, 1f, 1f));
    }

    /// <inheritdoc />
    protected override void OnDraw(IRenderer renderer) =>
        renderer.DrawQuad(GlobalRect, Color.Value);
}
