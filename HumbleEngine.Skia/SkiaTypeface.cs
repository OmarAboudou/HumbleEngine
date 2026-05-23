using SkiaSharp;

namespace HumbleEngine.Skia;

public class SkiaTypeface : ITypeface, IDisposable
{
    internal SKTypeface NativeTypeface { get; }

    public string FamilyName => NativeTypeface.FamilyName;
    public bool   IsBold     => NativeTypeface.IsBold;
    public bool   IsItalic   => NativeTypeface.IsItalic;

    public SkiaTypeface(SKTypeface typeface)
    {
        NativeTypeface = typeface;
    }

    public static SkiaTypeface FromFamilyName(string family, bool bold = false, bool italic = false)
    {
        var style = (bold, italic) switch
        {
            (true,  true)  => SKFontStyle.BoldItalic,
            (true,  false) => SKFontStyle.Bold,
            (false, true)  => SKFontStyle.Italic,
            _              => SKFontStyle.Normal,
        };
        return new SkiaTypeface(SKTypeface.FromFamilyName(family, style));
    }

    public void Dispose() => NativeTypeface.Dispose();
}
