using Silk.NET.Windowing;
using SlkWindow = Silk.NET.Windowing.Window;
using HmblWindow = HumbleEngine.Core.Window;

namespace HumbleEngine.Silk;

public class SilkWindow : HmblWindow
{
    private IWindow window;

    public override void Initialize() 
        => window = SlkWindow.Create(
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