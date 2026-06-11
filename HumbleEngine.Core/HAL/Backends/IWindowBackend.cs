namespace HumbleEngine;

public interface IWindowBackend : ISurfaceBackend
{
    IWindow CreateWindow(WindowDescription description);
}
