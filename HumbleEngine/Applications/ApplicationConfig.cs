namespace HumbleEngine;

public record ApplicationConfig
{
    public string Title  { get; init; } = "HumbleEngine";
    public int    Width  { get; init; } = 1280;
    public int    Height { get; init; } = 720;

    public GPUBackend PreferredBackend { get; init; } = GPUBackend.OpenGL;

    // Passes built-in — désactivables pour des apps event-driven
    public bool EnableUpdate       { get; init; } = true;
    public bool EnableFixedUpdate  { get; init; } = true;
    public bool EnableReconciler   { get; init; } = true;
    public bool EnableLayout       { get; init; } = true;
    public bool EnablePaint        { get; init; } = true;

    public Node? Scene { get; init; }
}
