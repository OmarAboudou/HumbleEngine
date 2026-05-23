namespace HumbleEngine;

public record ApplicationConfig(
    Node          Scene,
    WindowOptions WindowOptions
)
{
    public static ApplicationConfig Default(Node scene)
        => new(scene, WindowOptions.Default);
}
