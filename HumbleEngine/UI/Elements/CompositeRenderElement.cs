using System.Collections;

namespace HumbleEngine;

public abstract record CompositeRenderElement : RenderElement, IEnumerable
{
    private ListProperty<RenderElement> PrivateChildren { get; init; } = new();
    public ReadOnlyListProperty<RenderElement> Children => PrivateChildren.AsReadOnly();
    
    public void Add(RenderElement e)
        => PrivateChildren.Add(e);

    public void Add(IReadOnlyList<RenderElement> e)
    {
        foreach (RenderElement renderElement in e) 
            Add(renderElement);
    }

    public IEnumerator GetEnumerator() => PrivateChildren.GetEnumerator();

}
