namespace HumbleEngine.Sandbox;

/// <summary>
/// A UI node that draws a generated checkerboard texture (roadmap 10, Bloc 2 —
/// the texture proof: an image on a quad before any glyph). The RGBA pixels are
/// built once on the CPU; the GPU texture follows the tree lifecycle like the
/// triangle's mesh — acquired in <see cref="OnAttached"/> from the context
/// renderer, released in <see cref="OnDetached"/>.
/// </summary>
public sealed class ImageNode : UINode
{
    private const int Texels = 64;
    private readonly byte[] _pixels = BuildCheckerboard();
    private ITexture? _texture;

    /// <inheritdoc />
    protected override void OnAttached() =>
        _texture = Renderer!.CreateTexture(_pixels, Texels, Texels, TextureFormat.Rgba8);

    /// <inheritdoc />
    protected override void OnDetached()
    {
        _texture?.Dispose();
        _texture = null;
    }

    /// <inheritdoc />
    protected override void OnDraw(IRenderer renderer) =>
        renderer.DrawTexturedQuad(GlobalRect, _texture!, new Rect(0f, 0f, 1f, 1f), new Vector4(1f, 1f, 1f, 1f));

    /// <summary>An 8×8 checkerboard of two colours, expanded to <see cref="Texels"/>² RGBA texels.</summary>
    private static byte[] BuildCheckerboard()
    {
        var pixels = new byte[Texels * Texels * 4];
        for (var y = 0; y < Texels; y++)
        {
            for (var x = 0; x < Texels; x++)
            {
                var dark = ((x / 8) + (y / 8)) % 2 == 0;
                var i = (y * Texels + x) * 4;
                pixels[i + 0] = dark ? (byte)40  : (byte)230;
                pixels[i + 1] = dark ? (byte)60  : (byte)180;
                pixels[i + 2] = dark ? (byte)120 : (byte)90;
                pixels[i + 3] = 255;
            }
        }
        return pixels;
    }
}
