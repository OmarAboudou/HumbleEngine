using HumbleEngine;
using HumbleEngine.Sample;

using var app = new Application("HumbleEngine — Demo", 1000, 700);
app.Root = new DemoScreen();
app.Run();
