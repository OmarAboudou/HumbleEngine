namespace HumbleEngine;

public record ApplicationConfig
{
    public string     Title           { get; init; } = "HumbleEngine";
    public int        Width           { get; init; } = 1280;
    public int        Height          { get; init; } = 720;
    public GPUBackend PreferredBackend { get; init; } = GPUBackend.OpenGL;
    public Node?      Scene           { get; init; }
}
