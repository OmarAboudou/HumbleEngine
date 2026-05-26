using System.Collections;

namespace HumbleEngine.Core;

public abstract partial record MultipleChildrenWidget<TChildren> : Widget, IEnumerable<TChildren>
    where TChildren : Widget
{
    
    public void Add(TChildren child)
     => Children.Add(child);

    public void Add(IEnumerable<TChildren> children)
    {
        foreach (TChildren child in children)
            Add(child);
    }
    
    [WidgetProperty(WidgetRefreshFlag.LAYOUT | WidgetRefreshFlag.PAINT)]
    public partial ListProperty<TChildren> Children { get; init; }
    
    public IEnumerator<TChildren> GetEnumerator()
        => Children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() 
        => GetEnumerator();
}