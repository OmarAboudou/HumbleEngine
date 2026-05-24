namespace HumbleEngine;

public record ApplicationConfig(
    Node          Scene,
    WindowOptions WindowOptions
)
{
    public GraphicsAPI          Api    { get; init; } = GraphicsAPI.OpenGL;
    public IReadOnlyList<IPass> Passes { get; init; } = [];

    public static ApplicationConfig Default(Node scene)
        => new(scene, WindowOptions.Default) { Passes = [new FixedUpdatePass()] };
}
