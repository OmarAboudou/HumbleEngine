namespace HumbleEngine.Core.UIs;

[Flags]
public enum WidgetRefreshFlag
{
    NONE = 0,
    PAINT = 1 << 0,
    LAYOUT = 1 << 1,
}