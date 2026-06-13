namespace Reconciliation;

/// <summary>
/// The retained, reconciled tree node — the counterpart of a (throwaway) Widget
/// that survives across rebuilds. It holds the current Widget, the persistent
/// <see cref="State"/> (created once, at mount), and the reconciled child elements.
/// </summary>
public sealed class Element
{
    private readonly List<Element> _children = [];
    private readonly BuildOwner _owner;
    private bool _dirty;

    internal Element(Widget widget, BuildOwner owner)
    {
        Widget = widget;
        _owner = owner;
        // State is created ONCE, here, at mount. A later Update reuses it — that is
        // exactly why state survives a parent rebuild.
        if (widget is StatefulWidget sw)
        {
            State = sw.CreateState();
            State.Element = this;
        }
    }

    public Widget Widget { get; private set; }
    public State? State { get; private set; }
    public IReadOnlyList<Element> Children => _children;

    internal void MarkDirty()
    {
        if (_dirty)
            return;
        _dirty = true;
        _owner.ScheduleDirty(this);
    }

    internal void Rebuild() => Update(Widget);

    /// <summary>Adopts a (possibly new) widget and reconciles the subtree under it.</summary>
    internal void Update(Widget newWidget)
    {
        Widget = newWidget;
        _dirty = false;
        Reconcile(DescribeChildren());
    }

    private IReadOnlyList<Widget> DescribeChildren() => Widget switch
    {
        StatefulWidget => [State!.Build()],
        StatelessWidget s => [s.Build()],
        Column c => c.Children,
        _ => [], // Text and other leaves
    };

    /// <summary>
    /// Positional reconciliation: child i of the old tree is matched with child i of
    /// the new description. If they "can update" (same type + key), the element is
    /// REUSED (its State lives on); otherwise it is unmounted and a fresh one mounted
    /// (its State is lost). No keys yet → reordering a list misattributes state.
    /// </summary>
    private void Reconcile(IReadOnlyList<Widget> newChildren)
    {
        var result = new List<Element>(newChildren.Count);
        for (var i = 0; i < newChildren.Count; i++)
        {
            var newWidget = newChildren[i];
            var old = i < _children.Count ? _children[i] : null;
            if (old is not null && CanUpdate(old.Widget, newWidget))
            {
                old.Update(newWidget); // reused → its State is preserved
                result.Add(old);
            }
            else
            {
                old?.Unmount();
                result.Add(_owner.Mount(newWidget));
            }
        }

        for (var i = newChildren.Count; i < _children.Count; i++)
            _children[i].Unmount();

        _children.Clear();
        _children.AddRange(result);
    }

    private void Unmount()
    {
        foreach (var child in _children)
            child.Unmount();
        _children.Clear();
        State = null;
    }

    private static bool CanUpdate(Widget oldWidget, Widget newWidget) =>
        oldWidget.GetType() == newWidget.GetType() && Equals(oldWidget.Key, newWidget.Key);

    /// <summary>Flattens the retained tree to text — our stand-in for rendering.</summary>
    public string Render() =>
        Widget is Text text
            ? text.Value
            : string.Join("\n", _children.Select(c => c.Render()));
}

/// <summary>Drives mounting and flushes dirty rebuilds — the spike's mini "engine".</summary>
public sealed class BuildOwner
{
    private readonly List<Element> _dirty = [];

    public Element MountRoot(Widget widget) => Mount(widget);

    internal Element Mount(Widget widget)
    {
        var element = new Element(widget, this);
        element.Update(widget);
        return element;
    }

    internal void ScheduleDirty(Element element) => _dirty.Add(element);

    /// <summary>Re-runs build on the dirty elements; reconciliation preserves child state.</summary>
    public void FlushDirty()
    {
        var batch = _dirty.ToArray();
        _dirty.Clear();
        foreach (var element in batch)
            element.Rebuild();
    }
}
