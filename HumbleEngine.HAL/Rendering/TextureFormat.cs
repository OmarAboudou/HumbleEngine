namespace HumbleEngine;

/// <summary>
/// Pixel layout of the bytes handed to <see cref="IRenderer.CreateTexture"/>.
/// The backend maps each to a concrete GPU format and reads the bytes as a raw
/// copy — no translation.
/// </summary>
public enum TextureFormat
{
    /// <summary>Four 8-bit channels, red first: a colour image (one texel = 4 bytes).</summary>
    Rgba8,

    /// <summary>
    /// One 8-bit channel: a coverage/alpha map (one texel = 1 byte). The format
    /// FreeType rasterizes glyphs into — the übershader reads it as alpha.
    /// </summary>
    R8,
}
