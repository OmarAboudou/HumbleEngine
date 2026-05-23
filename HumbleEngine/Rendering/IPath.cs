namespace HumbleEngine;

public interface IPath
{
    void MoveTo(float x, float y);
    void LineTo(float x, float y);
    void CubicTo(float x1, float y1, float x2, float y2, float x3, float y3);
    void QuadTo(float x1, float y1, float x2, float y2);
    void ArcTo(Rect oval, float startAngle, float sweepAngle, bool forceMoveTo);
    void AddRect(Rect rect);
    void AddRoundRect(RoundRect rrect);
    void AddOval(Rect oval);
    void Close();
    void Reset();
}
