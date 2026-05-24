namespace HumbleEngine.Demo.UI;

public class NavItemNode : UINode
{
    private readonly string _label;
    private readonly bool   _active;
    private bool            _hovered;

    public NavItemNode(string label, bool active = false)
    {
        _label  = label;
        _active = active;
    }

    protected override RenderElement Render()
    {
        Color bg    = _active  ? new Color(239, 246, 255)
                    : _hovered ? new Color(248, 250, 252)
                    : Color.Transparent;
        Color color = _active  ? new Color(37, 99, 235)
                               : new Color(71, 85, 105);

        var item = new HLayout
        {
            Width          = Length.Fill,
            Height         = Length.Px(36),
            Padding        = EdgeInsets.Symmetric(horizontal: 16),
            CrossAlignment = CrossAlignment.Center,
            CornerRadius   = CornerRadius.All(6),
            Background     = bg,
            Margin         = new EdgeInsets(top: 0, right: 8, bottom: 0, left: 8),
        };
        item.Add(new Text(_label) { Font = new Font(14f), Color = color });
        return item
            .OnMouseEnter(() => { _hovered = true;  MarkDirty(); })
            .OnMouseExit (() => { _hovered = false; MarkDirty(); });
    }
}
