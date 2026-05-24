namespace HumbleEngine;

public interface IRenderer
{
    void Attach(IViewport viewport);
    void Detach();

    IReadOnlySignal<ICanvas> OnBeginFrame { get; }
    IReadOnlySignal         OnEndFrame   { get; }

    IPaint CreatePaint();
    IFont  CreateFont(ITypeface? typeface, float size);

    IShader CreateLinearGradient(Vector2<float> start, Vector2<float> end, Color[] colors, float[]? positions = null);
    IShader CreateRadialGradient(Vector2<float> center, float radius,      Color[] colors, float[]? positions = null);
}
