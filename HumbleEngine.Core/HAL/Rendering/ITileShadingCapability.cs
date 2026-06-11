namespace HumbleEngine;

/// <summary>
/// Optional tile shading capability.
/// Detected via the <c>is</c> pattern so renderers that do not support it
/// are not required to implement it.
/// </summary>
public interface ITileShadingCapability
{
    /// <summary>Dispatches a tile shader with the given tile dimensions.</summary>
    void DispatchTileShader(TileShadingDescription description);
}

/// <summary>Parameters for a tile shading pass.</summary>
/// <param name="TileWidth">Tile width in pixels.</param>
/// <param name="TileHeight">Tile height in pixels.</param>
public record TileShadingDescription(int TileWidth, int TileHeight);
