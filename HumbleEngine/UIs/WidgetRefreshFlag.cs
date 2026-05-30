namespace HumbleEngine;

[Flags]
public enum WidgetRefreshFlag
{
    NONE = 0,
    LAYOUT = 1 << 1,
    PAINT = 1 << 2,
}