namespace HumbleEngine;

public class InputPass : IUpdatePass
{
    // ElementId → OnMouseExit callback stored from the frame the element was entered
    private Dictionary<ElementId, Action?> _hovered    = new();
    private bool                           _wasLeftDown = false;

    public void Execute(Node root, double delta, BlackBoard board)
    {
        var cache = board.Get<UILayoutCache>();
        if (cache is null) return;

        var viewport = (root as IRootNode)?.Viewport;
        if (viewport is null || viewport.Input.Mice.Count == 0) return;

        var mouse = viewport.Input.Mice[0];
        var pos   = mouse.Position;

        var nowHovered = new Dictionary<ElementId, (Action? Enter, Action? Exit, Action? Click)>();
        foreach (var (_, layout) in cache.Roots)
            Collect(layout, pos, nowHovered);

        foreach (var (id, cbs) in nowHovered)
            if (!_hovered.ContainsKey(id))
                cbs.Enter?.Invoke();

        foreach (var (id, exitCb) in _hovered)
            if (!nowHovered.ContainsKey(id))
                exitCb?.Invoke();

        _hovered.Clear();
        foreach (var (id, cbs) in nowHovered)
            _hovered[id] = cbs.Exit;

        bool isLeftDown = mouse.IsButtonPressed(MouseButton.Left);
        if (_wasLeftDown && !isLeftDown)
            foreach (var (_, cbs) in nowHovered)
                cbs.Click?.Invoke();
        _wasLeftDown = isLeftDown;
    }

    private static void Collect(LayoutNode node, Vector2<float> pos,
        Dictionary<ElementId, (Action?, Action?, Action?)> result)
    {
        if (!node.Box.Contains(pos)) return;
        var el = node.Element;
        if (el.OnMouseEnter != null || el.OnMouseExit != null || el.OnClick != null)
            result[node.Id] = (el.OnMouseEnter, el.OnMouseExit, el.OnClick);
        foreach (var child in node.Children)
            Collect(child, pos, result);
    }
}
