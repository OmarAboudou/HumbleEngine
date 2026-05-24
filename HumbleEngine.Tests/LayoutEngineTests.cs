using NUnit.Framework;

namespace HumbleEngine.Tests;

[TestFixture]
public class LayoutEngineTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private record Leaf      : RenderElement { }
    private record Container : CompositeRenderElement { }

    private static LayoutNode Layout(RenderElement root, float vw = 800, float vh = 600)
        => new LayoutEngine().Layout(root, vw, vh);

    private static LayoutBox Box(LayoutNode node) => node.Box;

    private static void AssertBox(LayoutBox box, float x, float y, float w, float h)
    {
        Assert.That(box.X,      Is.EqualTo(x).Within(0.01f), "X");
        Assert.That(box.Y,      Is.EqualTo(y).Within(0.01f), "Y");
        Assert.That(box.Width,  Is.EqualTo(w).Within(0.01f), "Width");
        Assert.That(box.Height, Is.EqualTo(h).Within(0.01f), "Height");
    }

    // ── Leaf ───────────────────────────────────────────────────────────────────

    [Test]
    public void Leaf_PxDimensions_BoxMatchesDeclaredSize()
    {
        var node = Layout(new Leaf() with { Width = Length.Px(100), Height = Length.Px(50) });
        AssertBox(Box(node), 0, 0, 100, 50);
    }

    [Test]
    public void Leaf_FillDimensions_UsesFullAvailable()
    {
        var node = Layout(new Leaf() with { Width = Length.Fill, Height = Length.Fill });
        AssertBox(Box(node), 0, 0, 800, 600);
    }

    [Test]
    public void Leaf_AutoDimensions_FillsAvailable()
    {
        var node = Layout(new Leaf()); // default Width/Height = Auto
        AssertBox(Box(node), 0, 0, 800, 600);
    }

    [Test]
    public void Leaf_Margin_OffsetsPositionAndReducesAvailableForFill()
    {
        var node = Layout(new Leaf() with
        {
            Width  = Length.Fill,
            Height = Length.Fill,
            Margin = EdgeInsets.All(10),
        });
        AssertBox(Box(node), 10, 10, 780, 580);
    }

    [Test]
    public void Leaf_MinWidth_ClampsSmallSize()
    {
        var node = Layout(new Leaf() with { Width = Length.Px(10), MinWidth = Length.Px(50) });
        Assert.That(node.Box.Width, Is.EqualTo(50).Within(0.01f));
    }

    [Test]
    public void Leaf_MaxWidth_ClampsLargeSize()
    {
        var node = Layout(new Leaf() with { Width = Length.Fill, MaxWidth = Length.Px(200) });
        Assert.That(node.Box.Width, Is.EqualTo(200).Within(0.01f));
    }

    [Test]
    public void Leaf_PercentWidth_ResolvesAgainstAvailable()
    {
        var node = Layout(new Leaf() with { Width = Length.Percent(50), Height = Length.Px(10) });
        Assert.That(node.Box.Width, Is.EqualTo(400).Within(0.01f));
    }

    // ── VLayout ────────────────────────────────────────────────────────────────

    [Test]
    public void VLayout_TwoChildren_StackedVertically()
    {
        var layout = new VLayout
        {
            new Leaf() with { Width = Length.Px(100), Height = Length.Px(50) },
            new Leaf() with { Width = Length.Px(100), Height = Length.Px(80) },
        };

        var node = Layout(layout);

        AssertBox(node.Children[0].Box, 0,   0, 100,  50);
        AssertBox(node.Children[1].Box, 0,  50, 100,  80);
    }

    [Test]
    public void VLayout_Gap_AddsSpaceBetweenChildren()
    {
        var layout = new VLayout { Gap = 20 };
        layout.Add(new Leaf() with { Width = Length.Px(100), Height = Length.Px(50) });
        layout.Add(new Leaf() with { Width = Length.Px(100), Height = Length.Px(80) });

        var node = Layout(layout);

        Assert.That(node.Children[0].Box.Y, Is.EqualTo(0).Within(0.01f));
        Assert.That(node.Children[1].Box.Y, Is.EqualTo(70).Within(0.01f)); // 50 + 20
    }

    [Test]
    public void VLayout_FillChild_GetsRemainingHeight()
    {
        var layout = new VLayout { Height = Length.Px(400), Gap = 0 };
        layout.Add(new Leaf() with { Height = Length.Px(100) });
        layout.Add(new Leaf() with { Height = Length.Fill });

        var node = Layout(layout);

        Assert.That(node.Children[1].Box.Height, Is.EqualTo(300).Within(0.01f));
        Assert.That(node.Children[1].Box.Y,      Is.EqualTo(100).Within(0.01f));
    }

    [Test]
    public void VLayout_TwoFillChildren_ShareRemainingEqually()
    {
        var layout = new VLayout { Height = Length.Px(600), Gap = 0 };
        layout.Add(new Leaf() with { Height = Length.Fill });
        layout.Add(new Leaf() with { Height = Length.Fill });

        var node = Layout(layout);

        Assert.That(node.Children[0].Box.Height, Is.EqualTo(300).Within(0.01f));
        Assert.That(node.Children[1].Box.Height, Is.EqualTo(300).Within(0.01f));
        Assert.That(node.Children[1].Box.Y,      Is.EqualTo(300).Within(0.01f));
    }

    [Test]
    public void VLayout_Padding_ShiftsChildrenIntoContentArea()
    {
        var layout = new VLayout
        {
            Width   = Length.Px(400),
            Height  = Length.Px(300),
            Padding = new EdgeInsets(top: 10, right: 0, bottom: 0, left: 20),
        };
        layout.Add(new Leaf() with { Width = Length.Px(50), Height = Length.Px(50) });

        var node = Layout(layout);

        Assert.That(node.Children[0].Box.X, Is.EqualTo(20).Within(0.01f));
        Assert.That(node.Children[0].Box.Y, Is.EqualTo(10).Within(0.01f));
    }

    [Test]
    public void VLayout_MainAlignment_Center_OffsetsChildrenToMiddle()
    {
        var layout = new VLayout
        {
            Height        = Length.Px(400),
            MainAlignment = MainAlignment.Center,
        };
        layout.Add(new Leaf() with { Height = Length.Px(100) });

        var node = Layout(layout);

        Assert.That(node.Children[0].Box.Y, Is.EqualTo(150).Within(0.01f)); // (400-100)/2
    }

    [Test]
    public void VLayout_MainAlignment_End_PushesChildrenToBottom()
    {
        var layout = new VLayout
        {
            Height        = Length.Px(400),
            MainAlignment = MainAlignment.End,
        };
        layout.Add(new Leaf() with { Height = Length.Px(100) });

        var node = Layout(layout);

        Assert.That(node.Children[0].Box.Y, Is.EqualTo(300).Within(0.01f)); // 400-100
    }

    [Test]
    public void VLayout_MainAlignment_SpaceBetween_DistributesFreely()
    {
        var layout = new VLayout
        {
            Height        = Length.Px(400),
            Gap           = 0,
            MainAlignment = MainAlignment.SpaceBetween,
        };
        layout.Add(new Leaf() with { Height = Length.Px(100) });
        layout.Add(new Leaf() with { Height = Length.Px(100) });

        var node = Layout(layout);

        Assert.That(node.Children[0].Box.Y, Is.EqualTo(0).Within(0.01f));
        Assert.That(node.Children[1].Box.Y, Is.EqualTo(300).Within(0.01f)); // 400-100
    }

    [Test]
    public void VLayout_MainAlignment_SpaceEvenly_DistributesIncludingEdges()
    {
        var layout = new VLayout
        {
            Height        = Length.Px(400),
            Gap           = 0,
            MainAlignment = MainAlignment.SpaceEvenly,
        };
        layout.Add(new Leaf() with { Height = Length.Px(100) });
        layout.Add(new Leaf() with { Height = Length.Px(100) });

        var node = Layout(layout);

        // free = 200, slots = 3, slot = 66.67
        float slot = 200f / 3f;
        Assert.That(node.Children[0].Box.Y, Is.EqualTo(slot).Within(0.01f));
        Assert.That(node.Children[1].Box.Y, Is.EqualTo(slot * 2 + 100).Within(0.01f));
    }

    [Test]
    public void VLayout_CrossAlignment_Center_CentersChildHorizontally()
    {
        var layout = new VLayout
        {
            Width          = Length.Px(400),
            CrossAlignment = CrossAlignment.Center,
        };
        layout.Add(new Leaf() with { Width = Length.Px(100), Height = Length.Px(50) });

        var node = Layout(layout);

        Assert.That(node.Children[0].Box.X, Is.EqualTo(150).Within(0.01f)); // (400-100)/2
    }

    [Test]
    public void VLayout_CrossAlignment_End_AlignsChildToRight()
    {
        var layout = new VLayout
        {
            Width          = Length.Px(400),
            CrossAlignment = CrossAlignment.End,
        };
        layout.Add(new Leaf() with { Width = Length.Px(100), Height = Length.Px(50) });

        var node = Layout(layout);

        Assert.That(node.Children[0].Box.X, Is.EqualTo(300).Within(0.01f)); // 400-100
    }

    [Test]
    public void VLayout_AutoHeight_ShrinksToSumOfChildren()
    {
        var layout = new VLayout { Gap = 10 };
        layout.Add(new Leaf() with { Height = Length.Px(50) });
        layout.Add(new Leaf() with { Height = Length.Px(80) });

        var node = Layout(layout);

        Assert.That(node.Box.Height, Is.EqualTo(140).Within(0.01f)); // 50+10+80
    }

    [Test]
    public void VLayout_AutoWidth_ShrinksToWidestChild()
    {
        var layout = new VLayout();
        layout.Add(new Leaf() with { Width = Length.Px(120), Height = Length.Px(10) });
        layout.Add(new Leaf() with { Width = Length.Px(200), Height = Length.Px(10) });

        var node = Layout(layout);

        Assert.That(node.Box.Width, Is.EqualTo(200).Within(0.01f));
    }

    // ── HLayout ────────────────────────────────────────────────────────────────

    [Test]
    public void HLayout_TwoChildren_PositionedHorizontally()
    {
        var layout = new HLayout
        {
            new Leaf() with { Width = Length.Px(100), Height = Length.Px(50) },
            new Leaf() with { Width = Length.Px(150), Height = Length.Px(50) },
        };

        var node = Layout(layout);

        AssertBox(node.Children[0].Box,   0, 0, 100, 50);
        AssertBox(node.Children[1].Box, 100, 0, 150, 50);
    }

    [Test]
    public void HLayout_FillChild_GetsRemainingWidth()
    {
        var layout = new HLayout { Width = Length.Px(500), Gap = 0 };
        layout.Add(new Leaf() with { Width = Length.Px(200) });
        layout.Add(new Leaf() with { Width = Length.Fill });

        var node = Layout(layout);

        Assert.That(node.Children[1].Box.Width, Is.EqualTo(300).Within(0.01f));
        Assert.That(node.Children[1].Box.X,     Is.EqualTo(200).Within(0.01f));
    }

    [Test]
    public void HLayout_CrossAlignment_Center_CentersChildVertically()
    {
        var layout = new HLayout
        {
            Height         = Length.Px(300),
            CrossAlignment = CrossAlignment.Center,
        };
        layout.Add(new Leaf() with { Width = Length.Px(50), Height = Length.Px(100) });

        var node = Layout(layout);

        Assert.That(node.Children[0].Box.Y, Is.EqualTo(100).Within(0.01f)); // (300-100)/2
    }

    [Test]
    public void HLayout_AutoWidth_ShrinksToSumOfChildren()
    {
        var layout = new HLayout { Gap = 10 };
        layout.Add(new Leaf() with { Width = Length.Px(100), Height = Length.Px(50) });
        layout.Add(new Leaf() with { Width = Length.Px(200), Height = Length.Px(50) });

        var node = Layout(layout);

        Assert.That(node.Box.Width, Is.EqualTo(310).Within(0.01f)); // 100+10+200
    }

    // ── Stack ──────────────────────────────────────────────────────────────────

    [Test]
    public void Stack_FullStretch_ChildFillsParent()
    {
        var stack = new Stack { Width = Length.Px(400), Height = Length.Px(300) };
        stack.Add(new Leaf() with { Anchor = Anchor.FullStretch });

        var node = Layout(stack);

        AssertBox(node.Children[0].Box, 0, 0, 400, 300);
    }

    [Test]
    public void Stack_TopLeft_ChildAtOriginWithOwnSize()
    {
        var stack = new Stack { Width = Length.Px(400), Height = Length.Px(300) };
        stack.Add(new Leaf() with
        {
            Anchor = Anchor.TopLeft,
            Width  = Length.Px(100),
            Height = Length.Px(50),
        });

        var node = Layout(stack);

        AssertBox(node.Children[0].Box, 0, 0, 100, 50);
    }

    [Test]
    public void Stack_Center_ChildCenteredInParent()
    {
        var stack = new Stack { Width = Length.Px(400), Height = Length.Px(300) };
        stack.Add(new Leaf() with
        {
            Anchor = Anchor.Center,
            Width  = Length.Px(100),
            Height = Length.Px(60),
        });

        var node = Layout(stack);

        // point anchor: top-left of child at (0.5*400, 0.5*300) = (200, 150)
        AssertBox(node.Children[0].Box, 200, 150, 100, 60);
    }

    [Test]
    public void Stack_TopStretch_ChildSpansFullWidth()
    {
        var stack = new Stack { Width = Length.Px(400), Height = Length.Px(300) };
        stack.Add(new Leaf() with
        {
            Anchor = Anchor.TopStretch,
            Height = Length.Px(50),
        });

        var node = Layout(stack);

        Assert.That(node.Children[0].Box.X,     Is.EqualTo(0).Within(0.01f));
        Assert.That(node.Children[0].Box.Width,  Is.EqualTo(400).Within(0.01f));
        Assert.That(node.Children[0].Box.Y,      Is.EqualTo(0).Within(0.01f));
        Assert.That(node.Children[0].Box.Height, Is.EqualTo(50).Within(0.01f));
    }
}
