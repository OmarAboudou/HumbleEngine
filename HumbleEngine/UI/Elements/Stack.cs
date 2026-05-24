namespace HumbleEngine;

/// <summary>
/// Superpose ses enfants. Chaque enfant se positionne via sa propriété <see cref="RenderElement.Anchor"/>.
/// </summary>
public record Stack : CompositeRenderElement
{
    public Stack(Action? onMouseEnter = null, Action? onMouseExit = null, Action? onClick = null)
    {
        OnMouseEnter = onMouseEnter;
        OnMouseExit  = onMouseExit;
        OnClick      = onClick;
    }
}
