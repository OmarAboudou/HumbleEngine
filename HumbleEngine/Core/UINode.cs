namespace HumbleEngine;

public abstract class UINode : Node
{
    private RenderElement? _cached;
    private bool           _dirty = true;

    protected void MarkDirty() => _dirty = true;

    public RenderElement GetElement()
    {
        if (_dirty)
        {
            _cached = Render();
            _dirty  = false;
        }
        return _cached!;
    }

    protected abstract RenderElement Render();
}
