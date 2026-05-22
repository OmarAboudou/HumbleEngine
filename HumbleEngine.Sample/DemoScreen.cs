using SkiaSharp;

namespace HumbleEngine.Sample;

public class DemoScreen : Column
{
    // ── ViewModel ────────────────────────────────────────────────────────────

    private sealed class DemoViewModel
    {
        public ReactiveProperty<int>    ClickCount = new(0);
        public ReactiveProperty<string> ClickLabel = new("Aucun clic");
        public ReactiveProperty<string> EchoText   = new("—");

        public DemoViewModel()
        {
            ClickCount.Connect(n =>
                ClickLabel.Value = n == 0 ? "Aucun clic" : $"{n} clic{(n > 1 ? "s" : "")}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Nœud feuille : Box colorée avec texte blanc — démontre les styles de Box
    private sealed class StyleCard : Node
    {
        private readonly string  _text;
        private readonly SKColor _bg;
        private readonly float   _radius;
        private readonly bool    _bordered;

        public StyleCard(string text, SKColor bg, float radius = 0f, bool bordered = false)
        { _text = text; _bg = bg; _radius = radius; _bordered = bordered; }

        protected override RenderDescription RenderContent()
        {
            var box = new Box
            {
                new Span(_text).Color(SKColors.White).FontSize(13f)
            }.Color(_bg).Padding(20f, 12f);

            if (_radius  > 0) box = box.CornerRadius(_radius);
            if (_bordered)    box = box.Border(new SKColor(255, 255, 255, 160), 4f);
            return box;
        }
    }

    private static Label MakeHeader(string text)
    {
        var l = new Label();
        l.Text.Value     = text;
        l.FontSize.Value = 17f;
        l.Color.Value    = new SKColor(50, 70, 150);
        return l;
    }

    private static Label MakeLabel(string text, SKColor? color = null)
    {
        var l = new Label();
        l.Text.Value  = text;
        l.Color.Value = color ?? SKColors.DarkSlateGray;
        return l;
    }

    private static Row MakeRow(params Node[] nodes)
    {
        var row = new Row();
        row.Spacing.Value = 14f;
        foreach (var n in nodes) row.Add(n);
        return row;
    }

    // ── Init ──────────────────────────────────────────────────────────────────

    public override void Init()
    {
        base.Init();
        Spacing.Value = 22f;

        var vm = new DemoViewModel();

        // ── Titre ─────────────────────────────────────────────────────────────
        var title = new Label();
        title.Text.Value     = "HumbleEngine — Demo";
        title.FontSize.Value = 26f;
        title.Color.Value    = new SKColor(20, 30, 90);

        // ── Boutons ───────────────────────────────────────────────────────────
        var btnClick    = new Button { Text = { Value = "Cliquer !" } };
        var labelClicks = MakeLabel("Aucun clic", new SKColor(50, 110, 200));
        labelClicks.Text.BindFrom(vm.ClickLabel);
        btnClick.Pressed.Connect(() => vm.ClickCount.Value++);

        // ── TextInput / Echo ──────────────────────────────────────────────────
        var input     = new TextInput { Placeholder = { Value = "Écrivez quelque chose..." } };
        var labelEcho = MakeLabel("—", SKColors.SlateGray);
        input.Text.Connect(t => vm.EchoText.Value = t.Length > 0 ? $"« {t} »" : "—");
        labelEcho.Text.BindFrom(vm.EchoText);

        var widgetsSection = new Column
        {
            MakeRow(MakeLabel("Bouton :"), btnClick, labelClicks),
            MakeRow(MakeLabel("Saisie :"), input),
            MakeRow(MakeLabel("Echo :"), labelEcho),
        };
        widgetsSection.Spacing.Value = 12f;

        // ── Mise en page ──────────────────────────────────────────────────────
        var colorsRow = new Row
        {
            new StyleCard("Tomate",  new SKColor(220, 70,  70)),
            new StyleCard("Vert",    new SKColor(60,  165, 85)),
            new StyleCard("Bleu",    new SKColor(50,  115, 215)),
            new StyleCard("Violet",  new SKColor(130, 65,  205)),
            new StyleCard("Orange",  new SKColor(225, 140, 40)),
        };
        colorsRow.Spacing.Value = 10f;

        // ── Styles de Box ─────────────────────────────────────────────────────
        var stylesRow = new Row
        {
            new StyleCard("Plat",              new SKColor(95,  95,  115)),
            new StyleCard("Arrondi",           new SKColor(95,  95,  115), radius: 10f),
            new StyleCard("Bordure",           new SKColor(95,  95,  115), bordered: true),
            new StyleCard("Arrondi + Bordure", new SKColor(95,  95,  115), radius: 10f, bordered: true),
        };
        stylesRow.Spacing.Value = 10f;

        // ── Assemblage ────────────────────────────────────────────────────────
        Add(title);
        Add(MakeHeader("Widgets"));
        Add(widgetsSection);
        Add(MakeHeader("Mise en page"));
        Add(colorsRow);
        Add(MakeHeader("Styles de Box"));
        Add(stylesRow);
    }
}
