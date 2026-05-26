namespace HumbleEngine.Core;

public abstract record StatelessWidget : IWidget
{
    public object? Key { get; init; }

    public abstract LayoutResult Layout(LayoutConstraints layoutConstraints);
}