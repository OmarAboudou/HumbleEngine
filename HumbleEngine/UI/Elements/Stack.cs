namespace HumbleEngine;

/// <summary>
/// Superpose ses enfants. Chaque enfant se positionne via sa propriété <see cref="RenderElement.Anchor"/>.
/// </summary>
public record Stack(IReadOnlyList<RenderElement> Children) : CompositeRenderElement(Children);
