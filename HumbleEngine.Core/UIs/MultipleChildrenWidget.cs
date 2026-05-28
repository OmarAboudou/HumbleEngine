using System.Collections;

namespace HumbleEngine.Core;

public abstract partial record MultipleChildrenWidget<TChildren> : Widget, IEnumerable<TChildren>
    where TChildren : Widget
{
    [WidgetProperty(WidgetRefreshFlag.LAYOUT | WidgetRefreshFlag.PAINT)]
    public partial ListProperty<TChildren> Children { get; init; }
    
    public void Add(TChildren widget) 
        => Children.Add(widget);

    public void Add(IEnumerable<TChildren> widgets)
    {
        foreach (TChildren widget in widgets)
            Add(widget);
    }

    public IEnumerator<TChildren> GetEnumerator()
        => Children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() 
        => GetEnumerator();
}