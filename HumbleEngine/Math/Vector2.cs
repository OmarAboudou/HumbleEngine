using System.Numerics;

namespace HumbleEngine;

public readonly record struct Vector2<T>(T X, T Y) where T : INumber<T>
{
    public static Vector2<T> Zero => new(T.Zero, T.Zero);
    public static Vector2<T> One  => new(T.One,  T.One);

    public static Vector2<T> operator +(Vector2<T> a, Vector2<T> b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2<T> operator -(Vector2<T> a, Vector2<T> b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2<T> operator *(Vector2<T> a, T scalar)     => new(a.X * scalar, a.Y * scalar);
    public static Vector2<T> operator /(Vector2<T> a, T scalar)     => new(a.X / scalar, a.Y / scalar);
}
