namespace HumbleEngine;

public abstract class UINode : Node
{
    private RenderElement?  _cached;
    private bool            _dirty      = true;
    private List<IAnimation>? _animations;

    protected void MarkDirty()                    => _dirty = true;
    internal  bool IsDirty                        => _dirty;
    protected void AddAnimation(IAnimation anim)  => (_animations ??= new()).Add(anim);

    internal void AdvanceAnimations(double delta)
    {
        if (_animations is null) return;
        bool any = false;
        foreach (var anim in _animations)
        {
            if (!anim.IsComplete)
            {
                anim.Advance(delta);
                any = true;
            }
        }
        if (any) MarkDirty();
    }

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
