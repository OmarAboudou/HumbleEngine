using System.Runtime.InteropServices;

namespace HumbleEngine.Text;

/// <summary>
/// P/Invoke surface for FreeType (<c>libfreetype.so.6</c>) — the minimum to
/// rasterize glyphs: init the library, load a face from memory, set a pixel
/// size, render one character, read its bitmap and metrics. The compositor of
/// fonts (layout, atlas) lives one floor up in <see cref="Font"/>.
/// <para>
/// The glyph data is reached through FreeType's big C structs. Rather than
/// mirror them whole, the records below map only the fields that matter, at
/// their byte offsets in the <b>64-bit</b> layout of a modern FreeType
/// (<c>FT_Long</c>/pointer = 8, <c>FT_Int</c> = 4, <c>FT_Short</c> = 2, natural
/// alignment). The offsets are the learning unit — a 32-bit or exotic build
/// would shift them.
/// </para>
/// </summary>
internal static class FreeTypeNative
{
    private const string Lib = "libfreetype.so.6";

    /// <summary>Load flag: rasterize the glyph to an 8-bit anti-aliased bitmap immediately (FT_LOAD_RENDER).</summary>
    internal const int LoadRender = 0x4;

    /// <summary>Byte offset of <c>FT_FaceRec.glyph</c> (the active glyph slot pointer) — see the class remarks.</summary>
    internal const int FaceGlyphSlotOffset = 152;

    /// <summary>Initializes a FreeType library instance.</summary>
    [DllImport(Lib)]
    internal static extern int FT_Init_FreeType(out IntPtr library);

    /// <summary>Destroys a library and everything it still owns (faces included).</summary>
    [DllImport(Lib)]
    internal static extern int FT_Done_FreeType(IntPtr library);

    /// <summary>
    /// Opens a face from a memory buffer — the embedded font bytes. The buffer
    /// must outlive the face (FreeType reads it lazily), so the caller pins it.
    /// </summary>
    [DllImport(Lib)]
    internal static extern int FT_New_Memory_Face(
        IntPtr library, IntPtr fileBase, long fileSize, long faceIndex, out IntPtr face);

    /// <summary>Destroys a face. Its library must outlive it.</summary>
    [DllImport(Lib)]
    internal static extern int FT_Done_Face(IntPtr face);

    /// <summary>Sets the face's rendering size in pixels (0 = same as the other axis).</summary>
    [DllImport(Lib)]
    internal static extern int FT_Set_Pixel_Sizes(IntPtr face, uint pixelWidth, uint pixelHeight);

    /// <summary>
    /// Loads and (with <see cref="LoadRender"/>) rasterizes the glyph for a
    /// character code into the face's glyph slot, found by the face's charmap.
    /// </summary>
    [DllImport(Lib)]
    internal static extern int FT_Load_Char(IntPtr face, ulong charCode, int loadFlags);
}

/// <summary>
/// Mirror of <c>FT_Vector</c> — a 2D vector in 26.6 fixed point (pixels × 64).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct FtVector
{
    public long X;
    public long Y;
}

/// <summary>
/// Mirror of <c>FT_Bitmap</c> — the rasterized glyph: its dimensions, the row
/// stride (<see cref="Pitch"/>, may be negative for bottom-up), and the 8-bit
/// coverage buffer (<c>pixel_mode</c> = GRAY for <see cref="FreeTypeNative.LoadRender"/>).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct FtBitmap
{
    public uint Rows;
    public uint Width;
    public int Pitch;
    public IntPtr Buffer;
    public ushort NumGrays;
    public byte PixelMode;
    public byte PaletteMode;
    public IntPtr Palette;
}

/// <summary>
/// Partial mirror of <c>FT_GlyphSlotRec</c> — only the fields a glyph upload
/// needs, at their 64-bit offsets (see <see cref="FreeTypeNative"/> remarks):
/// the pen advance, the rendered bitmap, and the bitmap's origin offset (bearing).
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct FtGlyphSlot
{
    /// <summary>Pen advance after this glyph, 26.6 fixed point — <c>X &gt;&gt; 6</c> for pixels.</summary>
    [FieldOffset(128)] public FtVector Advance;

    /// <summary>The rendered coverage bitmap.</summary>
    [FieldOffset(152)] public FtBitmap Bitmap;

    /// <summary>Horizontal distance from the pen to the bitmap's left edge, in pixels.</summary>
    [FieldOffset(192)] public int BitmapLeft;

    /// <summary>Vertical distance from the baseline up to the bitmap's top edge, in pixels.</summary>
    [FieldOffset(196)] public int BitmapTop;
}
