namespace HumbleEngine.Core;

public record ApplicationConfig(
    Node Scene,
    IReadOnlyList<IFixedUpdatePass> FixedUpdatePasses,
    IReadOnlyList<IRenderPass> RenderPasses
);