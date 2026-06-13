using HumbleEngine;
using HumbleEngine.Linux;
using HumbleEngine.Sandbox;

OS.Register(new LinuxOS());

var desktop = (DesktopOS)OS.Current;

// Two windowing backends side by side, one Vulkan backend serving both:
// the trio "window ↔ renderer ↔ tree" instantiated twice (roadmap 09).
var waylandBackend  = desktop.GetWindowBackend("Wayland");
var x11Backend      = desktop.GetWindowBackend("X11");
var graphicsBackend = desktop.GetGraphicsBackend("Vulkan");

waylandBackend.Initialize();
x11Backend.Initialize();
var waylandWindow = waylandBackend.CreateWindow(new WindowDescription("HumbleEngine — Wayland + Vulkan", 800, 600));
var x11Window     = x11Backend.CreateWindow(new WindowDescription("HumbleEngine — X11 + Vulkan", 800, 600));

graphicsBackend.Initialize();
var waylandRenderer = graphicsBackend.CreateRenderer(waylandWindow);
var x11Renderer     = graphicsBackend.CreateRenderer(x11Window);

// Same scene template, two instances, two worlds — each tree owns its renderer,
// each node acquires its GPU resources from the tree it enters.
var waylandScene = new SandboxScene { Name = "Wayland" };
var waylandTree  = new SceneTree(waylandRenderer) { Root = waylandScene, Clipboard = waylandWindow.Clipboard };
var x11Scene     = new SandboxScene { Name = "X11" };
var x11Tree      = new SceneTree(x11Renderer) { Root = x11Scene, Clipboard = x11Window.Clipboard };

// Bloc 3 roadmap 09 — the single input channel, demonstrated raw: every event
// logged as-is (record ToString), movements throttled to stay readable.
var movedLogClock = System.Diagnostics.Stopwatch.StartNew();
waylandWindow.OnInput += e => LogInput("Wayland", e);
x11Window.OnInput     += e => LogInput("X11", e);

// Bloc 4 — each window routes into its tree, the symmetric of rendering:
// hover brightens the column tiles, a left click disposes one and the
// Column restacks on its own.
waylandWindow.OnInput += waylandTree.RouteInput;
x11Window.OnInput     += x11Tree.RouteInput;

Console.WriteLine($"OS       : {OS.Current.Name}");
Console.WriteLine($"Windows  : {waylandBackend.Name} + {x11Backend.Name}");
Console.WriteLine($"Graphics : {graphicsBackend.Name}");
Console.WriteLine("Running. Triangles disappear after 5 s; arrows move the blue panel (Shift = faster);");
Console.WriteLine("hover/click the column tiles. Close either window to exit.");

var clock    = System.Diagnostics.Stopwatch.StartNew();
var fpsClock = System.Diagnostics.Stopwatch.StartNew();
var frames   = 0;

// The loop policy is the application's, one Step per window in the condition:
// each trio pumps its window then plays its own frame, and the loop leaves as
// soon as either window asks to close. Delegates are created once, not per turn.
Action waylandFrame = () => DriveTrio(waylandScene, waylandTree, waylandRenderer, phase: 1f);
Action x11Frame     = () => DriveTrio(x11Scene, x11Tree, x11Renderer, phase: -1f);

while (waylandWindow.Step(waylandFrame) && x11Window.Step(x11Frame))
{
    frames++;
    if (fpsClock.Elapsed.TotalSeconds >= 1)
    {
        var fps = frames / fpsClock.Elapsed.TotalSeconds;
        Console.WriteLine($"[fps] {fps:F0}");
        waylandWindow.SetTitle($"HumbleEngine — Wayland + Vulkan — {fps:F0} fps");
        x11Window.SetTitle($"HumbleEngine — X11 + Vulkan — {fps:F0} fps");
        fpsClock.Restart();
        frames = 0;
    }

    // Both windows suspended (fully occluded): nothing renders to throttle the
    // loop — yield so a backgrounded engine does not spin a core while it keeps
    // pumping events (input, clipboard).
    if (waylandWindow.IsSuspended && x11Window.IsSuspended)
        System.Threading.Thread.Sleep(10);
}

// Destruction in reverse creation order, trios first.
x11Tree.Dispose();
waylandTree.Dispose();
x11Renderer.Dispose();
waylandRenderer.Dispose();
graphicsBackend.Dispose();
x11Window.Dispose();
waylandWindow.Dispose();
x11Backend.Dispose();
waylandBackend.Dispose();
return;

void LogInput(string source, InputEvent inputEvent)
{
    if (inputEvent is PointerMoved)
    {
        if (movedLogClock.ElapsedMilliseconds < 500)
            return;
        movedLogClock.Restart();
    }
    Console.WriteLine($"[input] {source}: {inputEvent}");
}

// One trio's frame: animate (phase keeps the two worlds visibly independent),
// render, honour the 5 s triangle demo, flush the dispose queue.
void DriveTrio(SandboxScene scene, SceneTree tree, IRenderer renderer, float phase)
{
    var t = (float)clock.Elapsed.TotalSeconds;
    scene.BreathingSize.Value = new Vector2(150f, 60f + 40f * MathF.Sin(t * phase * 3f));

    // Typewriter: the label's text grows then resets — it re-measures and the
    // marker tile beside it slides (content sizing through the reactive layout).
    const string phrase = "Humble Engine — éàç 0123";
    scene.LabelText.Value = phrase[..(1 + (int)(t * 6f) % phrase.Length)];

    // Skip the whole render when no frame could be acquired (window not
    // presentable): the loop keeps pumping events and the clipboard keeps serving.
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
