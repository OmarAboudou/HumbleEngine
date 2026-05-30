using System.Numerics;

namespace HumbleEngine;

public abstract record PaintCommand;

// --- Formes remplies ---
public record FillRect(Rect Bounds, Color Color)                                              : PaintCommand;
public record FillRRect(Rect Bounds, BorderRadius Radius, Color Color)                        : PaintCommand;
public record FillOval(Rect Bounds, Color Color)                                              : PaintCommand;

// --- Contour ---
public record StrokeRRect(Rect Bounds, BorderRadius Radius, Color Color, float Thickness)     : PaintCommand;

// --- Ombre ---
public record DrawShadow(Rect Bounds, BorderRadius Radius, Color Color,
                         float BlurRadius, float SpreadRadius,
                         float OffsetX = 0f, float OffsetY = 0f)                             : PaintCommand;

// --- Clip (save implicite — refermer avec Pop) ---
public record PushClipRect(Rect Bounds)                                                       : PaintCommand;
public record PushClipRRect(Rect Bounds, BorderRadius Radius)                                 : PaintCommand;
public record PushClipOval(Rect Bounds)                                                       : PaintCommand;

// --- Calques (doivent être refermés par Pop) ---
public record PushOpacity(byte Alpha)                                                         : PaintCommand;
public record PushTransform(Matrix3x2 Matrix)                                                 : PaintCommand;
public record Pop()                                                                           : PaintCommand;
