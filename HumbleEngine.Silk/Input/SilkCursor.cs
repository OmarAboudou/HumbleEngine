using HumbleEngine;
using SilkRawImage = Silk.NET.Core.RawImage;
using SilkCur        = Silk.NET.Input.ICursor;
using SilkCursorMode = Silk.NET.Input.CursorMode;
using SilkCursorType = Silk.NET.Input.CursorType;
using SilkStdCursor  = Silk.NET.Input.StandardCursor;

namespace HumbleEngine.Silk;

public class SilkCursor : ICursor
{
    private readonly SilkCur _cursor;

    public SilkCursor(SilkCur cursor) => _cursor = cursor;

    public CursorType     Type           { get => ToCursorType(_cursor.Type);               set => _cursor.Type           = ToSilkType(value); }
    public StandardCursor StandardCursor { get => ToStandardCursor(_cursor.StandardCursor); set => _cursor.StandardCursor = ToSilkStd(value); }
    public CursorMode     CursorMode     { get => ToCursorMode(_cursor.CursorMode);         set => _cursor.CursorMode     = ToSilkMode(value); }
    public bool           IsConfined     { get => _cursor.IsConfined;                       set => _cursor.IsConfined     = value; }
    public int            HotspotX       { get => _cursor.HotspotX;                         set => _cursor.HotspotX       = value; }
    public int            HotspotY       { get => _cursor.HotspotY;                         set => _cursor.HotspotY       = value; }
    public RawImage Image
    {
        get => new(_cursor.Image.Width, _cursor.Image.Height, _cursor.Image.Pixels);
        set => _cursor.Image = new SilkRawImage(value.Width, value.Height, value.Pixels);
    }

    public bool IsSupported(CursorMode     mode)   => _cursor.IsSupported(ToSilkMode(mode));
    public bool IsSupported(StandardCursor cursor) => _cursor.IsSupported(ToSilkStd(cursor));

    private static CursorType ToCursorType(SilkCursorType t) => t switch
    {
        SilkCursorType.Custom => CursorType.Custom,
        _                     => CursorType.Standard,
    };

    private static SilkCursorType ToSilkType(CursorType t) => t switch
    {
        CursorType.Custom => SilkCursorType.Custom,
        _                 => SilkCursorType.Standard,
    };

    private static CursorMode ToCursorMode(SilkCursorMode m) => m switch
    {
        SilkCursorMode.Hidden   => CursorMode.Hidden,
        SilkCursorMode.Disabled => CursorMode.Disabled,
        SilkCursorMode.Raw      => CursorMode.Raw,
        _                       => CursorMode.Normal,
    };

    private static SilkCursorMode ToSilkMode(CursorMode m) => m switch
    {
        CursorMode.Hidden   => SilkCursorMode.Hidden,
        CursorMode.Disabled => SilkCursorMode.Disabled,
        CursorMode.Raw      => SilkCursorMode.Raw,
        _                   => SilkCursorMode.Normal,
    };

    private static StandardCursor ToStandardCursor(SilkStdCursor s) => s switch
    {
        SilkStdCursor.Arrow      => StandardCursor.Arrow,
        SilkStdCursor.IBeam      => StandardCursor.IBeam,
        SilkStdCursor.Crosshair  => StandardCursor.Crosshair,
        SilkStdCursor.Hand       => StandardCursor.Hand,
        SilkStdCursor.HResize    => StandardCursor.HResize,
        SilkStdCursor.VResize    => StandardCursor.VResize,
        SilkStdCursor.NwseResize => StandardCursor.NwseResize,
        SilkStdCursor.NeswResize => StandardCursor.NeswResize,
        SilkStdCursor.ResizeAll  => StandardCursor.ResizeAll,
        SilkStdCursor.NotAllowed => StandardCursor.NotAllowed,
        SilkStdCursor.Wait       => StandardCursor.Wait,
        SilkStdCursor.WaitArrow  => StandardCursor.WaitArrow,
        _                        => StandardCursor.Default,
    };

    private static SilkStdCursor ToSilkStd(StandardCursor s) => s switch
    {
        StandardCursor.Arrow      => SilkStdCursor.Arrow,
        StandardCursor.IBeam      => SilkStdCursor.IBeam,
        StandardCursor.Crosshair  => SilkStdCursor.Crosshair,
        StandardCursor.Hand       => SilkStdCursor.Hand,
        StandardCursor.HResize    => SilkStdCursor.HResize,
        StandardCursor.VResize    => SilkStdCursor.VResize,
        StandardCursor.NwseResize => SilkStdCursor.NwseResize,
        StandardCursor.NeswResize => SilkStdCursor.NeswResize,
        StandardCursor.ResizeAll  => SilkStdCursor.ResizeAll,
        StandardCursor.NotAllowed => SilkStdCursor.NotAllowed,
        StandardCursor.Wait       => SilkStdCursor.Wait,
        StandardCursor.WaitArrow  => SilkStdCursor.WaitArrow,
        _                         => SilkStdCursor.Default,
    };
}
