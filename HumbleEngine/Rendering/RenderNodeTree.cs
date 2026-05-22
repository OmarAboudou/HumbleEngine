using SkiaSharp;

namespace HumbleEngine;

public sealed class RenderNodeTree
{
    private readonly List<RenderNode> _nodes        = new();
    private readonly List<int>        _subtreeSizes = new();

    // Tableaux typés — un par RenderNodeKind avec données
    private readonly List<TextData> _textData = new();

    // --- Construction ---

    // Appelle root.Render() pour obtenir la description complète,
    // puis aplatit récursivement l'arbre de descriptions.
    public void Rebuild(Node root)
    {
        _nodes.Clear();
        _subtreeSizes.Clear();
        _textData.Clear();

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
            case RenderNodeKind.Text:
                _textData.Add(desc.Text);
                return _textData.Count - 1;
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
                case RenderNodeKind.Text:
                    PaintText(canvas, node.Bounds, _textData[node.Index]);
                    break;
                // Column et autres conteneurs n'ont pas de visuel propre
            }
        }
    }

    private static void PaintText(SKCanvas canvas, Rect bounds, TextData data)
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
