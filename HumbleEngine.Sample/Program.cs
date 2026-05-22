using HumbleEngine;
using SkiaSharp;

using var app = new Application("HumbleEngine — Phase 2", 800, 600);

app.Root = new Label
{
    Text     = { Value = "Hello, HumbleEngine!" },
    FontSize = { Value = 32f },
    Color    = { Value = SKColors.DarkSlateBlue }
};

app.Run();
