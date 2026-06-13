using System.Runtime.InteropServices;
using HumbleEngine.Text;

namespace HumbleEngine;

/// <summary>
/// A font face at one pixel size, backed by FreeType. Owns the library, the
/// face, and the pinned font bytes the face reads lazily — disposed in reverse.
/// Rasterizes one glyph at a time into the shared face slot; <see cref="Rasterize"/>
/// copies the result out before the next call reuses the slot.
/// </summary>
public sealed class Font : IDisposable
{
    private readonly IntPtr _library;
    private readonly IntPtr _face;
    private GCHandle _pin;
    private bool _disposed;

    /// <summary>Rendering size in pixels — the glyph bitmaps' scale.</summary>
    public int PixelSize { get; }

    /// <summary>
    /// Loads a face from in-memory font data at <paramref name="pixelSize"/>.
    /// The data is pinned for the face's lifetime (FreeType reads it lazily).
    /// </summary>
    /// <exception cref="InvalidOperationException">FreeType could not initialize or load the face.</exception>
    public Font(byte[] fontData, int pixelSize)
    {
        ArgumentNullException.ThrowIfNull(fontData);
        PixelSize = pixelSize;
        _pin = GCHandle.Alloc(fontData, GCHandleType.Pinned);

        try
        {
            Check(FreeTypeNative.FT_Init_FreeType(out _library), "FT_Init_FreeType");
            Check(FreeTypeNative.FT_New_Memory_Face(_library, _pin.AddrOfPinnedObject(), fontData.LongLength, 0, out _face),
                  "FT_New_Memory_Face");
            Check(FreeTypeNative.FT_Set_Pixel_Sizes(_face, 0, (uint)pixelSize), "FT_Set_Pixel_Sizes");
        }
        catch
        {
            if (_face != IntPtr.Zero) FreeTypeNative.FT_Done_Face(_face);
            if (_library != IntPtr.Zero) FreeTypeNative.FT_Done_FreeType(_library);
            if (_pin.IsAllocated) _pin.Free();
            throw;
        }
    }

    /// <summary>Loads the engine's default font (DejaVu Sans, embedded) at <paramref name="pixelSize"/>.</summary>
    public static Font Default(int pixelSize) => new(LoadEmbedded("Fonts/DejaVuSans.ttf"), pixelSize);

    /// <summary>
    /// Renders <paramref name="character"/> and copies its coverage bitmap and
    /// metrics out of the face slot. A blank glyph (e.g. space) yields an empty
    /// coverage with a real advance.
    /// </summary>
    /// <exception cref="InvalidOperationException">FreeType failed to load the glyph.</exception>
    internal RasterizedGlyph Rasterize(char character)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Check(FreeTypeNative.FT_Load_Char(_face, character, FreeTypeNative.LoadRender), $"FT_Load_Char '{character}'");

        var slot = Marshal.PtrToStructure<FtGlyphSlot>(
            Marshal.ReadIntPtr(_face, FreeTypeNative.FaceGlyphSlotOffset));

        var width = (int)slot.Bitmap.Width;
        var rows  = (int)slot.Bitmap.Rows;
        var advance = slot.Advance.X / 64f; // 26.6 fixed → pixels

        if (width == 0 || rows == 0)
            return new RasterizedGlyph([], 0, 0, slot.BitmapLeft, slot.BitmapTop, advance);

        // Copy row by row: pitch may exceed width (and be negative for bottom-up).
        var coverage = new byte[width * rows];
        for (var r = 0; r < rows; r++)
            Marshal.Copy(slot.Bitmap.Buffer + r * slot.Bitmap.Pitch, coverage, r * width, width);

        return new RasterizedGlyph(coverage, width, rows, slot.BitmapLeft, slot.BitmapTop, advance);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        FreeTypeNative.FT_Done_Face(_face);
        FreeTypeNative.FT_Done_FreeType(_library);
        _pin.Free();
    }

    /// <exception cref="InvalidOperationException">The resource is missing from the assembly.</exception>
    private static byte[] LoadEmbedded(string resourceName)
    {
        using var stream = typeof(Font).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded font '{resourceName}' not found.");
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    /// <summary>Throws on a non-zero FreeType error code.</summary>
    private static void Check(int error, string operation)
    {
        if (error != 0)
            throw new InvalidOperationException($"{operation} failed: FreeType error 0x{error:X}.");
    }
}

/// <summary>
/// One rasterized glyph in managed memory: the 8-bit coverage bitmap (row-major,
/// tightly packed) and its metrics — the Font→Atlas boundary, so the atlas never
/// touches FreeType's unmanaged buffers.
/// </summary>
internal readonly record struct RasterizedGlyph(
    byte[] Coverage, int Width, int Rows, int BitmapLeft, int BitmapTop, float Advance);
