using HumbleEngine;
using HumbleEngine.Linux;
using HumbleEngine.Sandbox;

// CLI: choose the OS, the windowing backend and the graphics backend.
//   dotnet run -- [--os Linux] [--windowing Wayland|X11] [--graphics Vulkan|OpenGL]
if (args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("Usage: HumbleEngine.Sandbox [--os Linux] [--windowing Wayland|X11] [--graphics Vulkan|OpenGL]");
    return;
}

var osName        = ArgValue("--os", "Linux");
var windowingName = ArgValue("--windowing", "Wayland");
var graphicsName  = ArgValue("--graphics", "Vulkan");

// The application registers the target platform. Only Linux ships today; the
// switch is where Windows/macOS slot in once their platform assemblies exist
// (and the project references them).
OS? platform = osName.ToLowerInvariant() switch
{
    "linux" => new LinuxOS(),
    _       => null,
};
if (platform is null)
{
    Console.Error.WriteLine($"Unknown OS '{osName}'. Available: Linux.");
    return;
}
OS.Register(platform);
var desktop = (DesktopOS)OS.Current;

// One trio "window ↔ renderer ↔ tree". The engine runs several side by side (the
// integration tests cover both backends), but the demo stays single-window: the
// multi-window event-pump decoupling — a blocking render must not freeze the
// other window's pump — is its own deferred roadmap (docs/roadmaps/11_boucle.md).
IWindowBackend   windowBackend;
IGraphicsBackend graphicsBackend;
try
{
    windowBackend   = desktop.GetWindowBackend(windowingName);
    graphicsBackend = desktop.GetGraphicsBackend(graphicsName);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine($"Available windowing: {string.Join(", ", desktop.AvailableWindowBackends.Select(b => b.Name))}");
    Console.Error.WriteLine($"Available graphics : {string.Join(", ", desktop.AvailableGraphicsBackends.Select(b => b.Name))}");
    return;
}

windowBackend.Initialize();
var window = windowBackend.CreateWindow(
    new WindowDescription($"HumbleEngine — {windowBackend.Name} + {graphicsBackend.Name}", 800, 600));

graphicsBackend.Initialize();
var renderer = graphicsBackend.CreateRenderer(window);

var scene = new SandboxScene { Name = "Sandbox" };
var tree  = new SceneTree(renderer) { Root = scene, Clipboard = window.Clipboard };

// The single input channel, demonstrated raw (every event logged, moves throttled),
// then wired into the tree — the symmetric of rendering.
var movedLogClock = System.Diagnostics.Stopwatch.StartNew();
window.OnInput += LogInput;
window.OnInput += tree.RouteInput;

Console.WriteLine($"OS       : {OS.Current.Name}");
Console.WriteLine($"Windowing: {windowBackend.Name}");
Console.WriteLine($"Graphics : {graphicsBackend.Name}");
Console.WriteLine("Running. Triangle disappears after 5 s; arrows move the blue panel (Shift = faster);");
Console.WriteLine("hover/click the column tiles; type, select and copy in the fields. Close the window to exit.");

var clock    = System.Diagnostics.Stopwatch.StartNew();
var fpsClock = System.Diagnostics.Stopwatch.StartNew();
var frames   = 0;

// One window: the loop is the Step sugar (pump, then frame) — the policy is the
// application's, as everywhere in the engine.
while (window.Step(Frame))
{
    frames++;
    if (fpsClock.Elapsed.TotalSeconds >= 1)
    {
        var fps = frames / fpsClock.Elapsed.TotalSeconds;
        Console.WriteLine($"[fps] {fps:F0}");
        window.SetTitle($"HumbleEngine — {windowBackend.Name} + {graphicsBackend.Name} — {fps:F0} fps");
        fpsClock.Restart();
        frames = 0;
    }
}

// Destruction in reverse creation order.
tree.Dispose();
renderer.Dispose();
graphicsBackend.Dispose();
window.Dispose();
windowBackend.Dispose();
return;

// Reads "--name value" from the command line, or returns the fallback.
string ArgValue(string name, string fallback)
{
    for (var i = 0; i + 1 < args.Length; i++)
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    return fallback;
}

void LogInput(InputEvent inputEvent)
{
    if (inputEvent is PointerMoved)
    {
        if (movedLogClock.ElapsedMilliseconds < 500)
            return;
        movedLogClock.Restart();
    }
    Console.WriteLine($"[input] {inputEvent}");
}

// Animate, render (skipping when the window is not presentable), honour the 5 s
// triangle demo, flush the dispose queue.
void Frame()
{
    var t = (float)clock.Elapsed.TotalSeconds;
    scene.BreathingSize.Value = new Vector2(150f, 60f + 40f * MathF.Sin(t * 3f));

    // Typewriter: the label's text grows then resets — it re-measures and the
    // marker tile beside it slides (content sizing through the reactive layout).
    const string phrase = "Humble Engine — éàç 0123";
    scene.LabelText.Value = phrase[..(1 + (int)(t * 6f) % phrase.Length)];

    if (renderer.BeginFrame())
    {
        tree.Render();
        renderer.EndFrame();
        renderer.Present();
    }

    if (t >= 5f)
        scene.DisposeTriangle(); // no-op once the triangle is gone
    tree.FlushDisposeQueue();
}
