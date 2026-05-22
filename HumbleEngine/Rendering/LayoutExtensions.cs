namespace HumbleEngine;

public static class LayoutExtensions
{
    public static T Width<T>(this T renderNode, float? width) where T : struct, IRenderNode
    {
        renderNode.Layout = renderNode.Layout with { Width = width };
        return renderNode;
    }

    public static T Height<T>(this T renderNode, float? height) where T : struct, IRenderNode
    {
        renderNode.Layout = renderNode.Layout with { Height = height };
        return renderNode;
    }

    public static T Padding<T>(this T renderNode, float x, float y) where T : struct, IRenderNode
    {
        renderNode.Layout = renderNode.Layout with { PaddingX = x, PaddingY = y };
        return renderNode;
    }

    public static T Padding<T>(this T renderNode, float uniform) where T : struct, IRenderNode
    {
        renderNode.Layout = renderNode.Layout with { PaddingX = uniform, PaddingY = uniform };
        return renderNode;
    }
}
