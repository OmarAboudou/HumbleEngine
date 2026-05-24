namespace HumbleEngine;

public record VLayout : FlowLayout
{
    public VLayout(Action? onMouseEnter = null, Action? onMouseExit = null, Action? onClick = null)
    {
        OnMouseEnter = onMouseEnter;
        OnMouseExit  = onMouseExit;
        OnClick      = onClick;
    }
}
