namespace HumbleEngine;

public readonly record struct EdgeInsets(float Left, float Top, float Right, float Bottom)
{
    public static EdgeInsets Zero                                          => new(0f, 0f, 0f, 0f);
    public static EdgeInsets All(float value)                             => new(value, value, value, value);
    public static EdgeInsets Symmetric(float horizontal = 0f, float vertical = 0f)
                                                                          => new(horizontal, vertical, horizontal, vertical);
    public static EdgeInsets Only(float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
                                                                          => new(left, top, right, bottom);

    public float Horizontal => Left + Right;
    public float Vertical   => Top + Bottom;
}
