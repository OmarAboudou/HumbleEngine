namespace HumbleEngine;

/// <summary>
/// The system clipboard, as text. Obtained from a window
/// (<see cref="IWindow.Clipboard"/>) — it carries the windowing connection, the
/// event pump and the input serial a copy needs. Per window: each backend owns
/// its own, so the desktop's two windows keep distinct clipboards.
/// <para>
/// Only UTF-8 plain text, only the CLIPBOARD selection (the X11 PRIMARY /
/// select-to-copy is deferred). <see cref="GetText"/> is a synchronous round-trip
/// to the current owner (the backend pumps until the answer, bounded).
/// </para>
/// </summary>
public interface IClipboard
{
    /// <summary>Takes ownership of the clipboard and offers <paramref name="text"/> to other applications.</summary>
    void SetText(string text);

    /// <summary>Reads the clipboard's text, or <c>null</c> when it is empty or holds no text.</summary>
    string? GetText();
}
