using HumbleEngine;
using HumbleEngine.Linux;
using HumbleEngine.Sandbox;

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

var scene = new SandboxScene(renderer) { Name = "Main" };
var tree  = new SceneTree { Root = scene };

Console.WriteLine($"OS       : {OS.Current.Name}");
Console.WriteLine($"Window   : {windowBackend.Name}");
Console.WriteLine($"Graphics : {graphicsBackend.Name}");
Console.WriteLine("Running. The triangle disappears after 5 s (QueueDispose demo). Close the window to exit.");

var clock    = System.Diagnostics.Stopwatch.StartNew();
var fpsClock = System.Diagnostics.Stopwatch.StartNew();
var frames   = 0;

window.Run(() =>
{
    renderer.BeginFrame();
    tree.Render(renderer);
    renderer.EndFrame();
    renderer.Present();

    frames++;
    if (fpsClock.Elapsed.TotalSeconds >= 1)
    {
        var fps = frames / fpsClock.Elapsed.TotalSeconds;
        Console.WriteLine($"[fps] {fps:F0}");
        window.SetTitle($"HumbleEngine — {windowBackendName} + {graphicsBackendName} — {fps:F0} fps");
        fpsClock.Restart();
        frames = 0;
    }

    if (clock.Elapsed.TotalSeconds >= 5)
        scene.DisposeTriangle(); // no-op once the triangle is gone

    tree.FlushDisposeQueue();
});

// Destruction in reverse creation order.
tree.Dispose();
renderer.Dispose();
graphicsBackend.Dispose();
window.Dispose();
windowBackend.Dispose();
