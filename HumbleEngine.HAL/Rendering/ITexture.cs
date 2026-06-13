namespace HumbleEngine;

/// <summary>
/// Opaque handle to a GPU-resident image, created by
/// <see cref="IRenderer.CreateTexture"/> and drawn through
/// <see cref="IRenderer.DrawTexturedQuad"/> on the renderer that created it.
/// <see cref="Width"/>/<see cref="Height"/> let the caller express a sub-region
/// (a glyph in an atlas) as a normalized rectangle. Owned by the caller: dispose
/// it before its renderer — the usual reverse creation order.
/// </summary>
public interface ITexture : IDisposable
{
    /// <summary>Width in texels.</summary>
    int Width { get; }

    /// <summary>Height in texels.</summary>
    int Height { get; }
}
