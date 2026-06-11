namespace HumbleEngine;

public interface ITileShadingCapability
{
    void DispatchTileShader(TileShadingDescription description);
}

public record TileShadingDescription(int TileWidth, int TileHeight);
