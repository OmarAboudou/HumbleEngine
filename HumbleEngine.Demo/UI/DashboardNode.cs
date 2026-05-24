using HumbleEngine;

namespace HumbleEngine.Demo.UI;

public class DashboardNode : UINode, IUpdate
{
    private int _frame;
    private int _clickCount;
    private string? _hoveredCard; // "border" | "bg" | null

    private readonly NavItemNode _navDashboard  = new("Dashboard", active: true);
    private readonly NavItemNode _navComponents = new("Components");
    private readonly NavItemNode _navSettings   = new("Settings");
    private readonly NavItemNode _navAbout      = new("About");

    public DashboardNode()
    {
        this.Attach(_navDashboard);
        this.Attach(_navComponents);
        this.Attach(_navSettings);
        this.Attach(_navAbout);
    }

    public void Update(double delta)
    {
        _frame++;
        MarkDirty();
    }

    protected override RenderElement Render()
    {
        return new VLayout { Header, Body, Footer }
            with { Background = new Color(241, 245, 249) };
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
        bar.Add(new Text("HumbleEngine") { Font = new Font(16f), Color = Color.White              });
        bar.Add(new Text("•")            { Font = new Font(14f), Color = new Color(71, 85, 105)   });
        bar.Add(new Text("Demo")         { Font = new Font(14f), Color = new Color(148, 163, 184) });
        return bar;
    }

    private RenderElement Body()
    {
        var body = new HLayout { Height = Length.Fill };
        body.Add(Sidebar);
        body.Add(Content);
        return body;
    }

    // NavItemNodes sont des UINodes embarqués directement dans le layout via NodeElement.
    private RenderElement Sidebar()
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
        panel.Add(_navDashboard);   // UINode → NodeElement implicitement
        panel.Add(_navComponents);
        panel.Add(_navSettings);
        panel.Add(SidebarLabel("Info"));
        panel.Add(_navAbout);
        return panel;
    }

    private RenderElement Content()
    {
        var area = new VLayout
        {
            Width   = Length.Fill,
            Height  = Length.Fill,
            Padding = EdgeInsets.All(28),
            Gap     = 20,
        };

        area.Add(new Text("Dashboard") { Font = new Font(22f), Color = new Color(15, 23, 42) });
        var layoutCards = new HLayout { Gap = 16 };
        layoutCards.Add(Card("VLayout",  "Vertical flow with\ngap & alignment",    new Color(239, 246, 255), new Color(59, 130, 246)));
        layoutCards.Add(Card("HLayout",  "Horizontal flow with\nFill distribution", new Color(240, 253, 244), new Color(22, 163, 74)));
        layoutCards.Add(Card("Stack",    "Anchor-based\npositioning",               new Color(254, 243, 199), new Color(202, 138, 4)));
        layoutCards.Add(Card("Text",     "Left / Center / Right\nalignment",        new Color(253, 242, 248), new Color(219, 39, 119)));
        area.Add(layoutCards);

        area.Add(new Text("Events") { Font = new Font(22f), Color = new Color(15, 23, 42) });
        var eventCards = new HLayout { Gap = 16 };
        eventCards.Add(HoverBorderCard());
        eventCards.Add(HoverBgCard());
        eventCards.Add(ClickCounterCard());
        area.Add(eventCards);

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

    // ── Events demo helpers ────────────────────────────────────────────────────

    private RenderElement HoverBorderCard()
    {
        bool hovered = _hoveredCard == "border";
        var card = new VLayout
        {
            Width        = Length.Px(170),
            Padding      = EdgeInsets.All(16),
            Gap          = 8,
            Background   = new Color(255, 255, 255),
            BorderColor  = hovered ? new Color(99, 102, 241) : new Color(226, 232, 240),
            BorderWidth  = 1f,
            CornerRadius = CornerRadius.All(8),
        };
        card.Add(new Text("Hover Border")                         { Font = new Font(14f), Color = new Color(99, 102, 241) });
        card.Add(new TextBlock("La bordure s'illumine au survol") { Font = new Font(12f), Color = new Color(71, 85, 105)  });
        return card
            .OnMouseEnter(() => { _hoveredCard = "border"; MarkDirty(); })
            .OnMouseExit (() => { _hoveredCard = null;     MarkDirty(); });
    }

    private RenderElement HoverBgCard()
    {
        bool hovered = _hoveredCard == "bg";
        var card = new VLayout
        {
            Width        = Length.Px(170),
            Padding      = EdgeInsets.All(16),
            Gap          = 8,
            Background   = hovered ? new Color(79, 70, 229) : new Color(99, 102, 241),
            CornerRadius = CornerRadius.All(8),
        };
        card.Add(new Text("Hover Background")                     { Font = new Font(14f), Color = Color.White              });
        card.Add(new TextBlock("Le fond s'assombrit\nau survol")  { Font = new Font(12f), Color = new Color(199, 210, 254) });
        return card
            .OnMouseEnter(() => { _hoveredCard = "bg"; MarkDirty(); })
            .OnMouseExit (() => { _hoveredCard = null; MarkDirty(); });
    }

    private RenderElement ClickCounterCard()
    {
        var card = new VLayout
        {
            Width        = Length.Px(170),
            Padding      = EdgeInsets.All(16),
            Gap          = 8,
            Background   = new Color(255, 255, 255),
            BorderColor  = new Color(226, 232, 240),
            BorderWidth  = 1f,
            CornerRadius = CornerRadius.All(8),
        };
        card.Add(new Text("Click Counter")               { Font = new Font(14f), Color = new Color(15, 23, 42)   });
        card.Add(new Text($"{_clickCount}")               { Font = new Font(28f), Color = new Color(99, 102, 241) });
        card.Add(new TextBlock("Clique pour incrémenter") { Font = new Font(12f), Color = new Color(71, 85, 105)  });
        return card.OnClick(() => { _clickCount++; MarkDirty(); });
    }

    // ── Shared helpers ─────────────────────────────────────────────────────────

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
        card.Add(new Text(title)     { Font = new Font(14f), Color = accent                 });
        card.Add(new TextBlock(body) { Font = new Font(12f), Color = new Color(71, 85, 105) });
        return card;
    }
}
