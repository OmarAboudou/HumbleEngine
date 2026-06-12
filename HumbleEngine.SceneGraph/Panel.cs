namespace HumbleEngine;

/// <summary>
/// The first concrete UI node: fills its rectangle with a colour — übershader
/// mode 0, alpha blended. The bread and butter of every UI: backgrounds,
/// separators, and (with future modes) the base of buttons and fields.
/// </summary>
public sealed class Panel : UINode
{
    /// <summary>RGBA colour of the filled rectangle. Opaque white by default.</summary>
    public Reactive<Vector4> Color { get; }

    public Panel()
    {
        Color = CreateReactive(new Vector4(1f, 1f, 1f, 1f));
    }

    /// <inheritdoc />
    protected override void OnDraw(IRenderer renderer) =>
        renderer.DrawQuad(GlobalRect, Color.Value);
}
