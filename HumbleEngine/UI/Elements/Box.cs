namespace HumbleEngine;

public record Box : CompositeRenderElement
{
    public Color BorderColor { get; init; }
    public float BorderWidth { get; init; }
}

public static class BoxExtensions
{
    public static Box BorderColor(this Box el, Color color) => el with { BorderColor = color };
    public static Box BorderWidth(this Box el, float width) => el with { BorderWidth = width };
}
