namespace HumbleEngine;

public static class PaintPass
{
    public static bool DebugFill { get; set; }

    private static readonly Color[] Palette =
    [
        new Color(220, 80,  80,  160),
        new Color(80,  200, 80,  160),
        new Color(80,  120, 220, 160),
        new Color(220, 200, 60,  160),
        new Color(200, 80,  200, 160),
        new Color(60,  200, 200, 160),
        new Color(220, 140, 60,  160),
    ];

    internal static void Paint(Widget root, PaintCommandBuffer buffer, Position parentOffset = default)
    {
        if (root is PrimitiveWidget primitive)
        {
            Position worldOffset = parentOffset + primitive.LocalPosition.Value;

            if (DebugFill)
                buffer.Add(new FillRect(
                    Rect.FromPositionAndSize(worldOffset, primitive.GetSize()),
                    DebugColorFor(primitive)));

            primitive.PaintBefore(buffer, worldOffset);

            foreach (Widget child in primitive.MountedChildren)
                Paint(child, buffer, worldOffset);

            primitive.PaintAfter(buffer, worldOffset);
        }
        else
        {
            foreach (Widget child in root.MountedChildren)
                Paint(child, buffer, parentOffset);
        }
    }

    private static Color DebugColorFor(Widget w)
    {
        int index = Math.Abs(w.GetType().Name.GetHashCode()) % Palette.Length;
        return Palette[index];
    }
}
