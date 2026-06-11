namespace HumbleEngine.Linux;

internal sealed class LinuxOS : DesktopOS
{
    public override string Name => "Linux";

    /// <summary>
    /// Wayland is listed first as it is the preferred backend on modern Linux.
    /// X11 is kept as a fallback.
    /// </summary>
    public override IReadOnlyList<IWindowBackend> AvailableWindowBackends { get; } =
        [new WaylandWindowBackend(), new X11WindowBackend()];

    public override IReadOnlyList<IGraphicsBackend> AvailableGraphicsBackends { get; } =
        [new VulkanGraphicsBackend(), new OpenGLGraphicsBackend()];

    public override IWindowBackend    DefaultWindowBackend    => AvailableWindowBackends[0];   // Wayland
    public override IGraphicsBackend  DefaultGraphicsBackend  => AvailableGraphicsBackends[0]; // Vulkan
}
