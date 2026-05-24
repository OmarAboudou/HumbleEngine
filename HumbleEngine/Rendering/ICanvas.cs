namespace HumbleEngine;

public interface ICanvas : ITextMeasurer
{
    void Save();
    void Restore();

    void SetMatrix(Matrix matrix);
    void Concat(Matrix matrix);

    void Clear(Color color);

    void DrawRect(Rect rect, IPaint paint);
    void DrawRoundRect(RoundRect rrect, IPaint paint);
    void DrawCircle(float cx, float cy, float radius, IPaint paint);
    void DrawLine(float x0, float y0, float x1, float y1, IPaint paint);
    void DrawPath(IPath path, IPaint paint);
    void DrawImage(IImage image, float x, float y, IPaint? paint = null);
    void DrawText(string text, float x, float y, Font font, IPaint paint);
}
