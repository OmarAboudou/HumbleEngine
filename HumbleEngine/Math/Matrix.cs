namespace HumbleEngine;

// Row-major 3x3 affine matrix (same layout as SkMatrix)
// [ ScaleX  SkewX   TransX ]
// [ SkewY   ScaleY  TransY ]
// [ Persp0  Persp1  Persp2 ]
public readonly struct Matrix
{
    public float ScaleX  { get; }
    public float SkewX   { get; }
    public float TransX  { get; }
    public float SkewY   { get; }
    public float ScaleY  { get; }
    public float TransY  { get; }
    public float Persp0  { get; }
    public float Persp1  { get; }
    public float Persp2  { get; }

    public Matrix(
        float scaleX, float skewX,  float transX,
        float skewY,  float scaleY, float transY,
        float persp0, float persp1, float persp2)
    {
        ScaleX = scaleX; SkewX  = skewX;  TransX = transX;
        SkewY  = skewY;  ScaleY = scaleY; TransY = transY;
        Persp0 = persp0; Persp1 = persp1; Persp2 = persp2;
    }

    public static readonly Matrix Identity = new(1, 0, 0,  0, 1, 0,  0, 0, 1);

    public static Matrix CreateTranslation(float dx, float dy)
        => new(1, 0, dx,  0, 1, dy,  0, 0, 1);

    public static Matrix CreateScale(float sx, float sy)
        => new(sx, 0, 0,  0, sy, 0,  0, 0, 1);

    public static Matrix CreateRotationDegrees(float degrees)
    {
        float rad = degrees * (float)(System.Math.PI / 180.0);
        float cos = (float)System.Math.Cos(rad);
        float sin = (float)System.Math.Sin(rad);
        return new(cos, -sin, 0,  sin, cos, 0,  0, 0, 1);
    }

    public static Matrix operator *(Matrix a, Matrix b) => new(
        a.ScaleX * b.ScaleX + a.SkewX  * b.SkewY  + a.TransX * b.Persp0,
        a.ScaleX * b.SkewX  + a.SkewX  * b.ScaleY + a.TransX * b.Persp1,
        a.ScaleX * b.TransX + a.SkewX  * b.TransY + a.TransX * b.Persp2,
        a.SkewY  * b.ScaleX + a.ScaleY * b.SkewY  + a.TransY * b.Persp0,
        a.SkewY  * b.SkewX  + a.ScaleY * b.ScaleY + a.TransY * b.Persp1,
        a.SkewY  * b.TransX + a.ScaleY * b.TransY + a.TransY * b.Persp2,
        a.Persp0 * b.ScaleX + a.Persp1 * b.SkewY  + a.Persp2 * b.Persp0,
        a.Persp0 * b.SkewX  + a.Persp1 * b.ScaleY + a.Persp2 * b.Persp1,
        a.Persp0 * b.TransX + a.Persp1 * b.TransY + a.Persp2 * b.Persp2
    );

    public Vector2<float> MapPoint(float x, float y)
    {
        float w = Persp0 * x + Persp1 * y + Persp2;
        return new(
            (ScaleX * x + SkewX  * y + TransX) / w,
            (SkewY  * x + ScaleY * y + TransY) / w
        );
    }

    public bool Equals(Matrix o) =>
        ScaleX == o.ScaleX && SkewX  == o.SkewX  && TransX == o.TransX &&
        SkewY  == o.SkewY  && ScaleY == o.ScaleY && TransY == o.TransY &&
        Persp0 == o.Persp0 && Persp1 == o.Persp1 && Persp2 == o.Persp2;
    public override bool Equals(object? obj) => obj is Matrix m && Equals(m);
    public override int GetHashCode() => HashCode.Combine(ScaleX, SkewX, TransX, SkewY, ScaleY, TransY);
    public static bool operator ==(Matrix a, Matrix b) => a.Equals(b);
    public static bool operator !=(Matrix a, Matrix b) => !a.Equals(b);
}
