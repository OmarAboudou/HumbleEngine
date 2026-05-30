namespace HumbleEngine;

internal interface IMultiChildWidget<TChildren>
    where TChildren : Widget
{
    public ListProperty<TChildren> Children { get; init; }
}