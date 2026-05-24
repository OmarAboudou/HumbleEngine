namespace HumbleEngine;

public abstract class UINode : Node
{
    private RenderElement? _cached;
    private bool           _dirty = true;

    protected void MarkDirty() => _dirty = true;
    internal  bool IsDirty   => _dirty;

    public RenderElement GetElement()
    {
        if (_dirty)
        {
            var el  = Render();
            _cached = el.Key == null ? el with { Key = this } : el;
            _dirty  = false;
        }
        return _cached!;
    }

    protected abstract RenderElement Render();
}
