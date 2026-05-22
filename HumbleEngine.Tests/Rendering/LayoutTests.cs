namespace HumbleEngine.Tests;

[TestFixture]
public class LayoutTests
{
    // Node dont le rendu est un Box de taille explicite — pas de Span, pas de SKFont.
    private class FixedSizeNode : Node
    {
        private readonly float _w, _h;
        public FixedSizeNode(float w, float h) { _w = w; _h = h; }

        protected override RenderDescription RenderContent() =>
            new Box().Width(_w).Height(_h);
    }

    private static (RenderTree tree, BoxConstraints constraints) BuildTree(Node root)
    {
        var tree = new RenderTree();
        tree.Rebuild(root);
        var constraints = BoxConstraints.Loose(new Size(1000, 1000));
        tree.Layout(constraints);
        return (tree, constraints);
    }

    // --- VLayout ---

    [Test]
    public void VLayout_TwoChildren_StacksVertically()
    {
        var col = new Column();
        var a   = new FixedSizeNode(100, 20);
        var b   = new FixedSizeNode(80,  30);
        col.AddChild(a);
        col.AddChild(b);

        BuildTree(col);

        Assert.That(a.ComputedBounds.Y, Is.EqualTo(0));
        Assert.That(b.ComputedBounds.Y, Is.EqualTo(20));
    }

    [Test]
    public void VLayout_Spacing_AppliedBetweenChildren()
    {
        var col = new Column();
        col.Spacing.Value = 10f;
        var a = new FixedSizeNode(100, 20);
        var b = new FixedSizeNode(100, 30);
        col.AddChild(a);
        col.AddChild(b);

        BuildTree(col);

        Assert.That(b.ComputedBounds.Y, Is.EqualTo(30)); // 20 + 10
    }

    [Test]
    public void VLayout_HugsContentWidth()
    {
        var col = new Column();
        col.AddChild(new FixedSizeNode(120, 20));
        col.AddChild(new FixedSizeNode(80,  20));

        BuildTree(col);

        Assert.That(col.ComputedBounds.Width, Is.EqualTo(120));
    }

    [Test]
    public void VLayout_Padding_OffsetChildren()
    {
        var col = new Column();
        // Pas de prop Padding sur Column Node — on teste via RenderContent direct
        var a = new FixedSizeNode(100, 20);
        col.AddChild(a);

        var tree = new RenderTree();
        // VLayout avec padding en RenderContent custom
        var root = new PaddedColumnNode(paddingX: 16, paddingY: 8, child: a);
        tree.Rebuild(root);
        tree.Layout(BoxConstraints.Loose(new Size(1000, 1000)));

        Assert.That(a.ComputedBounds.X, Is.EqualTo(16));
        Assert.That(a.ComputedBounds.Y, Is.EqualTo(8));
    }

    // --- HLayout ---

    [Test]
    public void HLayout_TwoChildren_StacksHorizontally()
    {
        var row = new Row();
        var a   = new FixedSizeNode(60, 20);
        var b   = new FixedSizeNode(80, 20);
        row.AddChild(a);
        row.AddChild(b);

        BuildTree(row);

        Assert.That(a.ComputedBounds.X, Is.EqualTo(0));
        Assert.That(b.ComputedBounds.X, Is.EqualTo(60));
    }

    [Test]
    public void HLayout_Spacing_AppliedBetweenChildren()
    {
        var row = new Row();
        row.Spacing.Value = 8f;
        var a = new FixedSizeNode(60, 20);
        var b = new FixedSizeNode(80, 20);
        row.AddChild(a);
        row.AddChild(b);

        BuildTree(row);

        Assert.That(b.ComputedBounds.X, Is.EqualTo(68)); // 60 + 8
    }

    [Test]
    public void HLayout_HugsContentHeight()
    {
        var row = new Row();
        row.AddChild(new FixedSizeNode(60, 40));
        row.AddChild(new FixedSizeNode(60, 25));

        BuildTree(row);

        Assert.That(row.ComputedBounds.Height, Is.EqualTo(40));
    }

    // Helper — Column avec padding pour tester l'offset
    private class PaddedColumnNode : Node
    {
        private readonly float _px, _py;
        private readonly Node  _child;
        public PaddedColumnNode(float paddingX, float paddingY, Node child)
        {
            _px = paddingX; _py = paddingY; _child = child;
        }
        protected override RenderDescription RenderContent()
        {
            VLayout layout = [_child.Render()];
            return layout.Padding(_px, _py);
        }
    }
}
