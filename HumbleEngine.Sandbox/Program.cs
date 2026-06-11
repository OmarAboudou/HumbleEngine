using HumbleEngine;
using HumbleEngine.Linux;

OS.Register(new LinuxOS());

var desktop = (DesktopOS)OS.Current;

// Use X11 explicitly — Wayland is the default but is not yet implemented.
var windowBackend   = desktop.GetWindowBackend("X11");
var graphicsBackend = desktop.GetGraphicsBackend("OpenGL");

windowBackend.Initialize();
var window = windowBackend.CreateWindow(new WindowDescription("HumbleEngine — OpenGL", 800, 600));

graphicsBackend.Initialize();
var renderer = graphicsBackend.CreateRenderer(window);

Console.WriteLine($"OS      : {OS.Current.Name}");
Console.WriteLine($"Window  : {windowBackend.Name}");
Console.WriteLine($"Graphics: {graphicsBackend.Name}");
Console.WriteLine("Running. Close the window to exit.");

window.Run(() =>
{
    renderer.BeginFrame();
    renderer.EndFrame();
    renderer.Present();
});

renderer.Dispose();
graphicsBackend.Dispose();
window.Dispose();
windowBackend.Dispose();
