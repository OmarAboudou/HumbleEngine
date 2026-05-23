using System.Collections.Generic;

namespace HumbleEngine;

public record ApplicationConfig(
    Node          Scene,
    WindowOptions WindowOptions
)
{
    public IReadOnlyList<IPass> Passes { get; init; } = [];

    public static ApplicationConfig Default(Node scene)
        => new(scene, WindowOptions.Default) { Passes = [new UpdatePass()] };
}
