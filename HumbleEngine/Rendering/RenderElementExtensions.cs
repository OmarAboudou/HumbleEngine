namespace HumbleEngine;

public static class RenderElementExtensions
{
    public static T Create<T>(ReadOnlySpan<RenderDescription> items)
        where T : struct, ICompositeRenderElement
    {
        var builder = new T();
        foreach (var item in items)
            builder.Add(item);
        return builder;
    }

    public static T Width<T>(this T node, float? width) where T : struct, IRenderElement
    {
        node.Layout = node.Layout with { Width = width };
        return node;
    }

    public static T Height<T>(this T node, float? height) where T : struct, IRenderElement
    {
        node.Layout = node.Layout with { Height = height };
        return node;
    }

    public static T Padding<T>(this T node, float x, float y) where T : struct, IRenderElement
    {
        node.Layout = node.Layout with { PaddingX = x, PaddingY = y };
        return node;
    }

    public static T Padding<T>(this T node, float uniform) where T : struct, IRenderElement
    {
        node.Layout = node.Layout with { PaddingX = uniform, PaddingY = uniform };
        return node;
    }
}
