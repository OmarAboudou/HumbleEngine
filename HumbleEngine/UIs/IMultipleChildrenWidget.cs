namespace HumbleEngine;

internal interface IMultipleChildrenWidget<TChildren>
    where TChildren : Widget
{
    public ListProperty<TChildren> Children { get; init; }
}