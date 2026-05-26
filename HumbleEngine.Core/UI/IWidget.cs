namespace HumbleEngine.Core;

public interface IWidget
{
    public object? Key { get; init; }
    
    public LayoutResult Layout(LayoutConstraints layoutConstraints);
    
}