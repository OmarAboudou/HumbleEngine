using SkiaSharp;

namespace HumbleEngine;

public sealed class RenderNodeTree
{
    private readonly List<RenderNode> _nodes        = new();
    private readonly List<int>        _subtreeSizes = new();

    // Tableaux typés — un par RenderNodeKind
    private readonly List<TextData> _textData = new();

    // --- Construction ---

    public void Rebuild(Node root)
    {
        _nodes.Clear();
        _subtreeSizes.Clear();
        _textData.Clear();

        Build(root, offsetX: 0, offsetY: 0);
    }

    private int Build(Node node, float offsetX, float offsetY)
    {
        // Réserve une place pour ce nœud
        int myIndex = _nodes.Count;
        _nodes.Add(default);
        _subtreeSizes.Add(0);

        // Bounds absolues = position relative au parent + offset accumulé
        float absX = offsetX + node.ComputedBounds.X;
        float absY = offsetY + node.ComputedBounds.Y;
        var   abs  = new Rect(absX, absY, node.ComputedBounds.Width, node.ComputedBounds.Height);

        // Récupère la description visuelle du nœud et stocke la donnée typée
        var data       = node.CreateRenderNode();
        int dataIndex  = StoreData(data);

        // Construit récursivement les enfants (ils s'ajoutent après ce nœud)
        int subtreeSize = 1;
        foreach (var child in node.Children)
            subtreeSize += Build(child, absX, absY);

        // Remplit la place réservée
        _nodes[myIndex]        = new RenderNode { Kind = data.Kind, Index = dataIndex, Bounds = abs };
        _subtreeSizes[myIndex] = subtreeSize;

        return subtreeSize;
    }

    private int StoreData(RenderNodeData data)
    {
        switch (data.Kind)
        {
            case RenderNodeKind.Text:
                _textData.Add(data.Text);
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

    // --- Navigation (utilitaire) ---

    // Itère les indices directs des enfants d'un nœud.
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
