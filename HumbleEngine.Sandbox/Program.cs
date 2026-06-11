using HumbleEngine;
using HumbleEngine.Linux;

OS.Register(new LinuxOS());

var desktop = (DesktopOS)OS.Current;

// Select backend from args: dotnet run -- Wayland  |  dotnet run -- X11
var backendName = args.Length > 0 ? args[0] : "Wayland";

var windowBackend = desktop.GetWindowBackend(backendName);

windowBackend.Initialize();
var window = windowBackend.CreateWindow(new WindowDescription($"HumbleEngine — {backendName}", 800, 600));

Console.WriteLine($"OS     : {OS.Current.Name}");
Console.WriteLine($"Window : {windowBackend.Name}");
Console.WriteLine("Running. Close the window to exit.");

window.Run(() => { });

window.Dispose();
windowBackend.Dispose();
