namespace HumbleEngine;

public interface ICursor
{
    CursorType     Type           { get; set; }
    StandardCursor StandardCursor { get; set; }
    CursorMode     CursorMode     { get; set; }
    bool           IsConfined     { get; set; }
    int            HotspotX       { get; set; }
    int            HotspotY       { get; set; }
    RawImage       Image          { get; set; }

    bool IsSupported(CursorMode     mode);
    bool IsSupported(StandardCursor cursor);
}
