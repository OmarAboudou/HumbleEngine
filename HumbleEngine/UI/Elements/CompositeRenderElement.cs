using System.Collections;

namespace HumbleEngine;

public abstract record CompositeRenderElement : RenderElement, IEnumerable
{
    private ListProperty<RenderElement> PrivateChildren { get; init; } = new();
    public ReadOnlyListProperty<RenderElement> Children => PrivateChildren.AsReadOnly();

    public void Add(RenderElement el)              => PrivateChildren.Add(el);
    public void Add(UINode node)                   => Add(new NodeElement(node));
    public void Add(Func<RenderElement> factory)   => Add(factory());

    public void Add(IEnumerable<RenderElement> elements)
    {
        foreach (var el in elements) Add(el);
    }

    public IEnumerator GetEnumerator() => PrivateChildren.GetEnumerator();

}
