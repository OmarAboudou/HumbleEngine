using Silk.NET.Windowing;

namespace HumbleEngine.Silk;

class Program
{
    static void Main(string[] args)
    {
        IWindow window = Window.Create(WindowOptions.Default);
        
        window.Run();
    }
}