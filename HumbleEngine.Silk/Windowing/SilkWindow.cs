using Silk.NET.Windowing;
using Window = Silk.NET.Windowing.Window;
using HumbleWindow = HumbleEngine.Core.Window;

namespace HumbleEngine.Silk;

public class SilkWindow : HumbleWindow
{
    private IWindow window;

    public override void Initialize() 
        => window = Window.Create(
            WindowOptions.Default with {
                Title = Title.Value
            }
        );

    public override void Run() 
        => window.Run();

    public override void Reset() 
        => window.Reset();

    protected override void SetTitle(string title) 
        => window.Title = title;
    
    
}