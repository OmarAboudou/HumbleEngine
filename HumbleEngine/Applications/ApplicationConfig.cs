namespace HumbleEngine;

public record ApplicationConfig(
    Node Scene,
    IReadOnlyList<IFixedUpdatePass> FixedUpdatePasses,
    IReadOnlyList<IUpdatePass> UnderPasses
);