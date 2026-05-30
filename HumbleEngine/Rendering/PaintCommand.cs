using System.Numerics;

namespace HumbleEngine;

public abstract record PaintCommand;

// --- Formes remplies ---
public record FillRect(Rect Bounds, Color Color)                              : PaintCommand;
public record FillRRect(Rect Bounds, float Radius, Color Color)               : PaintCommand;
public record FillOval(Rect Bounds, Color Color)                              : PaintCommand;

// --- Contour ---
public record StrokeRRect(Rect Bounds, float Radius, Color Color, float Thickness) : PaintCommand;

// --- Ombre ---
public record DrawShadow(Rect Bounds, float Radius, Color Color, float BlurRadius, float SpreadRadius) : PaintCommand;

// --- Clip ---
public record ClipRect(Rect Bounds)                                           : PaintCommand;
public record ClipRRect(Rect Bounds, float Radius)                            : PaintCommand;
public record ClipOval(Rect Bounds)                                           : PaintCommand;

// --- Calques (doivent être refermés par Pop) ---
public record PushOpacity(byte Alpha)                                         : PaintCommand;
public record PushTransform(Matrix3x2 Matrix)                                 : PaintCommand;
public record Pop()                                                           : PaintCommand;
