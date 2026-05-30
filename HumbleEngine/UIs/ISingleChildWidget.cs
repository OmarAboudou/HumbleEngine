namespace HumbleEngine;

internal interface ISingleChildWidget<TChild>
    where TChild : Widget
{
    public Property<TChild?> Child { get; init; }
}