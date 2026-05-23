using System.Collections.Generic;

namespace HumbleEngine;

public interface IMouse
{
    IReadOnlyList<MouseButton>  SupportedButtons { get; }
    IReadOnlyList<ScrollWheel>  ScrollWheels     { get; }
    Vector2<float>              Position         { get; set; }
    ICursor                     Cursor           { get; }
    int                         DoubleClickTime  { get; set; }
    int                         DoubleClickRange { get; set; }

    bool IsButtonPressed(MouseButton button);

    IReadOnlySignal<MouseButton>                 OnButtonDown  { get; }
    IReadOnlySignal<MouseButton>                 OnButtonUp    { get; }
    IReadOnlySignal<MouseButton, Vector2<float>> OnClick       { get; }
    IReadOnlySignal<MouseButton, Vector2<float>> OnDoubleClick { get; }
    IReadOnlySignal<Vector2<float>>              OnMove        { get; }
    IReadOnlySignal<ScrollWheel>                 OnScroll      { get; }
}
