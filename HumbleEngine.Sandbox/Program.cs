using HumbleEngine;
using HumbleEngine.Linux;

OS.Register(new LinuxOS());

var desktop = (DesktopOS)OS.Current;

// Select backends from args: dotnet run -- [Wayland|X11] [Vulkan|OpenGL]
var windowBackendName   = args.Length > 0 ? args[0] : "Wayland";
var graphicsBackendName = args.Length > 1 ? args[1] : "Vulkan";

var windowBackend   = desktop.GetWindowBackend(windowBackendName);
var graphicsBackend = desktop.GetGraphicsBackend(graphicsBackendName);

windowBackend.Initialize();
var window = windowBackend.CreateWindow(
    new WindowDescription($"HumbleEngine — {windowBackendName} + {graphicsBackendName}", 800, 600));

graphicsBackend.Initialize();
var renderer = graphicsBackend.CreateRenderer(window);

Console.WriteLine($"OS       : {OS.Current.Name}");
Console.WriteLine($"Window   : {windowBackend.Name}");
Console.WriteLine($"Graphics : {graphicsBackend.Name}");
Console.WriteLine("Running. Close the window to exit.");

window.Run(() =>
{
    renderer.BeginFrame();
    renderer.EndFrame();
    renderer.Present();
});

// Destruction in reverse creation order.
renderer.Dispose();
graphicsBackend.Dispose();
window.Dispose();
windowBackend.Dispose();
