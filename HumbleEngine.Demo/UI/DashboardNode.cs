using HumbleEngine;

namespace HumbleEngine.Demo.UI;

public class DashboardNode : UINode, IUpdate
{
    private int _frame;

    public void Update(double delta)
    {
        _frame++;
        MarkDirty();
    }

    protected override RenderElement Render()
    {
        return new VLayout{
            Header,
            Body,
            Footer
        }with
        {
            Background = new Color(241, 245, 249)
        };
    }

    // ── Sections ───────────────────────────────────────────────────────────────

    private static RenderElement Header()
    {
        var bar = new HLayout
        {
            Width          = Length.Fill,
            Height         = Length.Px(52),
            Background     = new Color(15, 23, 42),
            Padding        = EdgeInsets.Symmetric(horizontal: 24),
            CrossAlignment = CrossAlignment.Center,
            Gap            = 12,
        };
        bar.Add(new Text("HumbleEngine") { Font = new Font(16f), Color = Color.White                  });
        bar.Add(new Text("•")            { Font = new Font(14f), Color = new Color(71, 85, 105)       });
        bar.Add(new Text("Demo")         { Font = new Font(14f), Color = new Color(148, 163, 184)     });
        return bar;
    }

    private static RenderElement Body()
    {
        var body = new HLayout { Height = Length.Fill };
        body.Add(Sidebar());
        body.Add(Content());
        return body;
    }

    private static RenderElement Sidebar()
    {
        var panel = new VLayout
        {
            Width       = Length.Px(200),
            Background  = new Color(255, 255, 255),
            Padding     = EdgeInsets.Symmetric(vertical: 12, horizontal: 0),
            BorderWidth = 1f,
            BorderColor = new Color(226, 232, 240),
            Gap         = 2,
        };

        panel.Add(SidebarLabel("Navigation"));
        panel.Add(NavItem("Dashboard", active: true));
        panel.Add(NavItem("Components"));
        panel.Add(NavItem("Settings"));

        panel.Add(SidebarLabel("Info"));
        panel.Add(NavItem("About"));

        return panel;
    }

    private static RenderElement Content()
    {
        var area = new VLayout
        {
            Width   = Length.Fill,
            Height  = Length.Fill,
            Padding = EdgeInsets.All(28),
            Gap     = 20,
        };

        area.Add(new Text("Dashboard") { Font = new Font(22f), Color = new Color(15, 23, 42) });

        var cards = new HLayout { Gap = 16 };
        cards.Add(Card(
            "VLayout",
            "Vertical flow with\ngap & alignment",
            new Color(239, 246, 255),
            new Color(59, 130, 246)));
        cards.Add(Card(
            "HLayout",
            "Horizontal flow with\nFill distribution",
            new Color(240, 253, 244),
            new Color(22, 163, 74)));
        cards.Add(Card(
            "Stack",
            "Anchor-based\npositioning",
            new Color(254, 243, 199),
            new Color(202, 138, 4)));
        cards.Add(Card(
            "Text",
            "Left / Center / Right\nalignment",
            new Color(253, 242, 248),
            new Color(219, 39, 119)));
        area.Add(cards);

        return area;
    }

    private RenderElement Footer()
    {
        var bar = new HLayout
        {
            Width          = Length.Fill,
            Height         = Length.Px(32),
            Background     = new Color(30, 41, 59),
            Padding        = EdgeInsets.Symmetric(horizontal: 24),
            CrossAlignment = CrossAlignment.Center,
        };
        bar.Add(new Text($"Running  —  frame {_frame}") { Font = new Font(11f), Color = new Color(148, 163, 184) });
        return bar;
    }

    // ── Component helpers ──────────────────────────────────────────────────────

    private static RenderElement NavItem(string label, bool active = false)
    {
        var item = new HLayout
        {
            Width          = Length.Fill,
            Height         = Length.Px(36),
            Padding        = EdgeInsets.Symmetric(horizontal: 16),
            CrossAlignment = CrossAlignment.Center,
            CornerRadius   = CornerRadius.All(6),
            Background     = active ? new Color(239, 246, 255) : Color.Transparent,
            Margin         = new EdgeInsets(top: 0, right: 8, bottom: 0, left: 8),
        };
        item.Add(new Text(label)
        {
            Font  = new Font(14f),
            Color = active ? new Color(37, 99, 235) : new Color(71, 85, 105),
        });
        return item;
    }

    private static RenderElement SidebarLabel(string text)
    {
        var row = new HLayout
        {
            Height         = Length.Px(28),
            Padding        = new EdgeInsets(top: 0, right: 16, bottom: 0, left: 16),
            CrossAlignment = CrossAlignment.Center,
        };
        row.Add(new Text(text) { Font = new Font(11f), Color = new Color(148, 163, 184) });
        return row;
    }

    private static RenderElement Card(string title, string body, Color bg, Color accent)
    {
        var card = new VLayout
        {
            Width        = Length.Px(170),
            Padding      = EdgeInsets.All(16),
            Gap          = 8,
            Background   = bg,
            BorderColor  = new Color(accent.R, accent.G, accent.B, 100),
            BorderWidth  = 1f,
            CornerRadius = CornerRadius.All(8),
        };
        card.Add(new Text(title)      { Font = new Font(14f), Color = accent                 });
        card.Add(new TextBlock(body)  { Font = new Font(12f), Color = new Color(71, 85, 105) });
        return card;
    }
}
