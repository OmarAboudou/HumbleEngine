using System.Collections;

namespace HumbleEngine;

public abstract record CompositeRenderElement : RenderElement, IEnumerable
{
    protected ListProperty<RenderElement> Children { get; init; } = new();
    
    public void Add(RenderElement e)
        => Children.Add(e);

    public void Add(IReadOnlyList<RenderElement> e)
    {
        foreach (RenderElement renderElement in e) 
            Add(renderElement);
    }

    public IEnumerator GetEnumerator() => Children.GetEnumerator();

}
