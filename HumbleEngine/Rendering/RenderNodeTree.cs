using SkiaSharp;

namespace HumbleEngine;

public sealed class RenderNodeTree
{
    private readonly List<RenderNode> _nodes        = new();
    private readonly List<int>        _subtreeSizes = new();

    // Tableaux typés — indexés par RenderNode.Index
    private readonly List<SpanData>    _spanData    = new();
    private readonly List<BoxData>     _boxData     = new();
    private readonly List<LinearLayoutData> _linearLayoutData = new();

    // Tableaux parallèles — même index que _nodes
    private readonly List<LayoutData> _layoutData = new();
    private readonly List<Node?>      _owners     = new();

    // --- Construction ---

    public void Rebuild(Node root)
    {
        _nodes.Clear();
        _subtreeSizes.Clear();
        _spanData.Clear();
        _boxData.Clear();
        _linearLayoutData.Clear();
        _layoutData.Clear();
        _owners.Clear();

        var desc = root.Render();
        if (desc.Kind != RenderNodeKind.None)
            Flatten(desc, parentX: 0, parentY: 0);
    }

    private int Flatten(RenderDescription desc, float parentX, float parentY)
    {
        int myIndex = _nodes.Count;
        _nodes.Add(default);
        _subtreeSizes.Add(0);
        _layoutData.Add(desc.Layout);
        _owners.Add(desc.Owner);

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
            case RenderNodeKind.VLayout:
            case RenderNodeKind.HLayout:
                _linearLayoutData.Add(desc.LinearLayout);

                return _linearLayoutData.Count - 1;
            default:
                return -1;
        }
    }

    // --- Layout ---

    public void Layout(BoxConstraints constraints)
    {
        if (_nodes.Count == 0) return;
        LayoutNode(0, constraints, 0f, 0f);
        for (int i = 0; i < _nodes.Count; i++)
            if (_owners[i] is { } owner)
                owner.ComputedBounds = _nodes[i].Bounds;
    }

    private Size LayoutNode(int index, BoxConstraints constraints, float x, float y)
    {
        var node   = _nodes[index];
        var layout = _layoutData[index];
        Size size;

        switch (node.Kind)
        {
            case RenderNodeKind.Span:
            {
                var span = _spanData[node.Index];
                using var font = new SKFont(SKTypeface.Default, span.FontSize);
                float iw = font.MeasureText(span.Content ?? "");
                float ih = font.Metrics.Descent - font.Metrics.Ascent;
                size = constraints.Constrain(layout.Width ?? iw, layout.Height ?? ih);
                break;
            }
            case RenderNodeKind.Box:
                size = LayoutChildren(index, constraints, x, y, layout, ChildArrangement.Layer);
                break;

            case RenderNodeKind.VLayout:
                size = LayoutChildren(index, constraints, x, y, layout, ChildArrangement.Vertical);
                break;

            case RenderNodeKind.HLayout:
                size = LayoutChildren(index, constraints, x, y, layout, ChildArrangement.Horizontal);
                break;

            default:
                size = constraints.Constrain(layout.Width ?? 0, layout.Height ?? 0);
                break;
        }

        _nodes[index] = node with { Bounds = new Rect(x, y, size.Width, size.Height) };
        return size;
    }

    private enum ChildArrangement { Layer, Vertical, Horizontal }

    private Size LayoutChildren(int index, BoxConstraints constraints, float x, float y, LayoutData layout, ChildArrangement arrangement)
    {
        float contentMaxW = Math.Max(0, (layout.Width  ?? constraints.MaxWidth)  - layout.PaddingX * 2);
        float contentMaxH = Math.Max(0, (layout.Height ?? constraints.MaxHeight) - layout.PaddingY * 2);
        var childConstraints = BoxConstraints.Loose(new Size(contentMaxW, contentMaxH));

        float childX = x + layout.PaddingX;
        float childY = y + layout.PaddingY;
        float accW   = 0;
        float accH   = 0;
        bool  first  = true;

        foreach (int childIdx in ChildIndices(index))
        {
            float spacing = arrangement switch
            {
                ChildArrangement.Vertical or ChildArrangement.Horizontal
                    => first ? 0 : _linearLayoutData[_nodes[index].Index].Spacing,
                _ => 0
            };

            float cx = childX + (arrangement == ChildArrangement.Horizontal ? accW + spacing : 0);
            float cy = childY + (arrangement == ChildArrangement.Vertical   ? accH + spacing : 0);

            var childSize = LayoutNode(childIdx, childConstraints, cx, cy);

            switch (arrangement)
            {
                case ChildArrangement.Vertical:
                    accH += childSize.Height + (first ? 0 : _linearLayoutData[_nodes[index].Index].Spacing);
                    accW  = Math.Max(accW, childSize.Width);
                    break;
                case ChildArrangement.Horizontal:
                    accW += childSize.Width + (first ? 0 : _linearLayoutData[_nodes[index].Index].Spacing);
                    accH  = Math.Max(accH, childSize.Height);
                    break;
                case ChildArrangement.Layer:
                    accW = Math.Max(accW, childSize.Width);
                    accH = Math.Max(accH, childSize.Height);
                    break;
            }

            first = false;
        }

        float ownW = layout.Width  ?? (accW + layout.PaddingX * 2);
        float ownH = layout.Height ?? (accH + layout.PaddingY * 2);
        return constraints.Constrain(ownW, ownH);
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
