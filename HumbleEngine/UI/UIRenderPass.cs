namespace HumbleEngine;

public class UIRenderPass : IRenderPass
{
    private readonly LayoutEngine _layout = new();

    public void Execute(Node root, RenderContext context, BlackBoard board)
    {
        var viewport = (root as IRootNode)?.Viewport;
        float vw = viewport?.Size.X ?? 0f;
        float vh = viewport?.Size.Y ?? 0f;

        var rootUINodes = root.GetSubtreeDepthFirst()
            .OfType<UINode>()
            .Where(n => n.Parent.Value is not UINode);

        var cacheRoots = new List<(UINode, LayoutNode)>();
        foreach (var uiNode in rootUINodes)
        {
            var layoutNode = _layout.Layout(uiNode.GetElement(), vw, vh, context.Canvas);
            cacheRoots.Add((uiNode, layoutNode));
            Draw(layoutNode, context.Canvas, context.Renderer);
        }
        board.Set(new UILayoutCache(cacheRoots));
    }

    private void Draw(LayoutNode node, ICanvas canvas, IRenderer renderer)
    {
        var el  = node.Element;
        var box = node.Box;

        bool hasTransform = el.Transform != Matrix.Identity;
        if (hasTransform)
        {
            canvas.Save();
            canvas.Concat(
                Matrix.CreateTranslation(box.X,    box.Y) *
                el.Transform                               *
                Matrix.CreateTranslation(-box.X, -box.Y));
        }

        if (el.Background != Color.Transparent)
            DrawBackground(el, box, canvas, renderer);

        if (el.BorderWidth > 0f)
            DrawBorder(el, box, canvas, renderer);

        if (el is Text textEl)
            DrawText(textEl, box, canvas, renderer);
        else if (el is TextBlock tbEl)
            DrawTextBlock(tbEl, box, canvas, renderer);

        foreach (var child in node.Children)
            Draw(child, canvas, renderer);

        if (hasTransform)
            canvas.Restore();
    }

    private static void DrawBackground(RenderElement el, LayoutBox box, ICanvas canvas, IRenderer renderer)
    {
        using var paint   = renderer.CreatePaint();
        paint.Color       = Tinted(el.Background, el.Opacity);
        paint.Style       = PaintStyle.Fill;
        paint.IsAntialias = true;

        DrawRect(el.CornerRadius, box.ToRect(), canvas, paint);
    }

    private static void DrawBorder(RenderElement el, LayoutBox box, ICanvas canvas, IRenderer renderer)
    {
        using var paint   = renderer.CreatePaint();
        paint.Color       = Tinted(el.BorderColor, el.Opacity);
        paint.Style       = PaintStyle.Stroke;
        paint.StrokeWidth = el.BorderWidth;
        paint.IsAntialias = true;

        DrawRect(el.CornerRadius, box.ToRect(), canvas, paint);
    }

    private static void DrawTextBlock(TextBlock el, LayoutBox box, ICanvas canvas, IRenderer renderer)
    {
        using var paint = renderer.CreatePaint();
        paint.Color       = Tinted(el.Color, el.Opacity);
        paint.IsAntialias = true;

        var   lines      = canvas.BreakLines(el.Content, el.Font, box.Width);
        float lineHeight = el.Font.Size;

        for (int i = 0; i < lines.Length; i++)
        {
            float lineWidth = canvas.MeasureText(lines[i], el.Font);
            float x = el.Align switch
            {
                TextAlign.Center => box.X + (box.Width - lineWidth) / 2f,
                TextAlign.Right  => box.X +  box.Width - lineWidth,
                _                => box.X,
            };
            canvas.DrawText(lines[i], x, box.Y + lineHeight * (i + 1), el.Font, paint);
        }
    }

    private static void DrawText(Text el, LayoutBox box, ICanvas canvas, IRenderer renderer)
    {
        using var paint = renderer.CreatePaint();
        paint.Color       = Tinted(el.Color, el.Opacity);
        paint.IsAntialias = true;

        float textWidth = canvas.MeasureText(el.Content, el.Font);
        float x = el.Align switch
        {
            TextAlign.Center => box.X + (box.Width - textWidth) / 2f,
            TextAlign.Right  => box.X +  box.Width - textWidth,
            _                => box.X,
        };

        canvas.DrawText(el.Content, x, box.Y + el.Font.Size, el.Font, paint);
    }

    private static void DrawRect(CornerRadius cr, Rect rect, ICanvas canvas, IPaint paint)
    {
        if (cr == CornerRadius.Zero)
            canvas.DrawRect(rect, paint);
        else
            canvas.DrawRoundRect(new RoundRect(rect, cr.TopLeft), paint);
    }

    private static Color Tinted(Color color, float opacity)
        => new(color.R, color.G, color.B, (byte)(color.A * opacity));
}
