namespace HumbleEngine.Core;

public abstract class StatefulWidget : IWidget
{
    public object? Key { get; init; }
    
    internal IWidget? CachedWidget;
    
    public IWidget GetWidget()
    {
        if (CachedWidget != null)
            return CachedWidget;
        
        CachedWidget = Build();
        return CachedWidget;
    }

    public bool IsDirty() 
        => CachedWidget == null;
    
    public void MarkDirty()
        => CachedWidget = null;

    public LayoutResult Layout(LayoutConstraints layoutConstraints)
        => GetWidget().Layout(layoutConstraints);

    protected abstract IWidget Build();
}