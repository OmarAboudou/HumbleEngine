using System.Collections.Generic;

namespace HumbleEngine;

public interface IKeyboard
{
    IReadOnlyList<Key> SupportedKeys  { get; }
    string             ClipboardText  { get; set; }

    bool IsKeyPressed(Key key);
    bool IsScancodePressed(int scancode);

    void BeginInput();
    void EndInput();

    IReadOnlySignal<Key, int> OnKeyDown { get; }
    IReadOnlySignal<Key, int> OnKeyUp   { get; }
    IReadOnlySignal<char>     OnKeyChar { get; }
}
