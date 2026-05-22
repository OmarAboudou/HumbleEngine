using SkiaSharp;

namespace HumbleEngine;

public sealed class RenderNodeTree
{
    private readonly List<RenderNode> _nodes        = new();
    private readonly List<int>        _subtreeSizes = new();

    // Tableaux typés — un par RenderNodeKind avec données
    private readonly List<SpanData> _spanData = new();
    private readonly List<BoxData>  _boxData  = new();

    // --- Construction ---

    // Appelle root.Render() pour obtenir la description complète,
    // puis aplatit récursivement l'arbre de descriptions.
    public void Rebuild(Node root)
    {
        _nodes.Clear();
        _subtreeSizes.Clear();
        _spanData.Clear();
        _boxData.Clear();

        var desc = root.Render();
        if (desc.Kind != RenderNodeKind.None)
            Flatten(desc, parentX: 0, parentY: 0);
    }

    private int Flatten(RenderDescription desc, float parentX, float parentY)
    {
        int myIndex = _nodes.Count;
        _nodes.Add(default);
        _subtreeSizes.Add(0);

        // Convertit les bounds relatives (parent-relative) en absolues
        float absX = parentX + desc.Bounds.X;
        float absY = parentY + desc.Bounds.Y;
        var   abs  = new Rect(absX, absY, desc.Bounds.Width, desc.Bounds.Height);

        int dataIndex = StoreData(desc);

        int subtreeSize = 1;
        if (desc.Children is not null)
            foreach (var child in desc.Children)
                subtreeSize += Flatten(child, absX, absY);

        _nodes[myIndex]        = new RenderNode { Kind = desc.Kind, Index = dataIndex, Bounds = abs };
        _subtreeSizes[myIndex] = subtreeSize;

        return subtreeSize;
    }

    private int StoreData(RenderDescription desc)
    {
        switch (desc.Kind)
        {
            case RenderNodeKind.Span:
                _spanData.Add(desc.Span);
                return _spanData.Count - 1;
            case RenderNodeKind.Box:
                _boxData.Add(desc.Box);
                return _boxData.Count - 1;
            default:
                return -1;
        }
    }

    // --- Rendu ---

    public void Paint(SKCanvas canvas)
    {
        for (int i = 0; i < _nodes.Count; i++)
        {
            var node = _nodes[i];
            switch (node.Kind)
            {
                case RenderNodeKind.Span:
                    PaintSpan(canvas, node.Bounds, _spanData[node.Index]);
                    break;
                case RenderNodeKind.Box:
                    PaintBox(canvas, node.Bounds, _boxData[node.Index]);
                    break;
                // Column n'a pas de visuel propre
            }
        }
    }

    private static void PaintBox(SKCanvas canvas, Rect bounds, BoxData data)
    {
        using var paint = new SKPaint { Color = data.BackgroundColor, IsAntialias = true };
        var rect = new SKRect(bounds.X, bounds.Y, bounds.X + bounds.Width, bounds.Y + bounds.Height);

        if (data.CornerRadius > 0)
            canvas.DrawRoundRect(rect, data.CornerRadius, data.CornerRadius, paint);
        else
            canvas.DrawRect(rect, paint);
    }

    private static void PaintSpan(SKCanvas canvas, Rect bounds, SpanData data)
    {
        if (string.IsNullOrEmpty(data.Content)) return;

        using var font  = new SKFont(SKTypeface.Default, data.FontSize);
        using var paint = new SKPaint { Color = data.Color, IsAntialias = true };

        canvas.DrawText(data.Content, bounds.X, bounds.Y - font.Metrics.Ascent, font, paint);
    }

    // --- Navigation ---

    public IEnumerable<int> ChildIndices(int parentIndex)
    {
        int end        = parentIndex + _subtreeSizes[parentIndex];
        int childIndex = parentIndex + 1;
        while (childIndex < end)
        {
            yield return childIndex;
            childIndex += _subtreeSizes[childIndex];
        }
    }
}
