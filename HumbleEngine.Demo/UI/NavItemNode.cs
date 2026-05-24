namespace HumbleEngine.Demo.UI;

public class NavItemNode : UINode
{
    private readonly string _label;
    private readonly bool   _active;
    private          bool   _hovered;

    private readonly Tween<Color> _bg;
    private readonly Tween<Color> _fg;

    public NavItemNode(string label, bool active = false)
    {
        _label  = label;
        _active = active;

        Color initBg = active ? new Color(239, 246, 255) : Color.Transparent;
        Color initFg = active ? new Color(37, 99, 235)   : new Color(71, 85, 105);

        _bg = new Tween<Color>(initBg, Lerp.Color, duration: 0.15f, easing: Easing.EaseOut);
        _fg = new Tween<Color>(initFg, Lerp.Color, duration: 0.15f, easing: Easing.EaseOut);

        AddAnimation(_bg);
        AddAnimation(_fg);
    }

    protected override RenderElement Render()
    {
        Color targetBg = _active  ? new Color(239, 246, 255)
                       : _hovered ? new Color(248, 250, 252)
                       : Color.Transparent;
        Color targetFg = _active  ? new Color(37, 99, 235)
                                  : new Color(71, 85, 105);

        _bg.To(targetBg);
        _fg.To(targetFg);

        var item = new HLayout
        {
            Width          = Length.Fill,
            Height         = Length.Px(36),
            Padding        = EdgeInsets.Symmetric(horizontal: 16),
            CrossAlignment = CrossAlignment.Center,
            CornerRadius   = CornerRadius.All(6),
            Background     = _bg.Value,
            Margin         = new EdgeInsets(top: 0, right: 8, bottom: 0, left: 8),
        };
        item.Add(new Text(_label) { Font = new Font(14f), Color = _fg.Value });
        return item
            .OnMouseEnter(() => { _hovered = true;  MarkDirty(); })
            .OnMouseExit (() => { _hovered = false; MarkDirty(); });
    }
}
