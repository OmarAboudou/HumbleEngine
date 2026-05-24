namespace HumbleEngine;

public interface IPaint : IDisposable
{
    Color       Color      { get; set; }
    PaintStyle  Style      { get; set; }
    float       StrokeWidth { get; set; }
    bool        IsAntialias { get; set; }
    IShader?    Shader     { get; set; }
}

public enum PaintStyle
{
    Fill,
    Stroke,
    StrokeAndFill,
}
