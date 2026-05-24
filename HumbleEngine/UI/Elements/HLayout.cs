namespace HumbleEngine;

public record HLayout : FlowLayout
{
    public HLayout(Action? onMouseEnter = null, Action? onMouseExit = null, Action? onClick = null)
    {
        OnMouseEnter = onMouseEnter;
        OnMouseExit  = onMouseExit;
        OnClick      = onClick;
    }
}
