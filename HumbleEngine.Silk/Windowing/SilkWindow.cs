using Silk.NET.Windowing;
using SlkWindow = Silk.NET.Windowing.Window;
using HmblWindow = HumbleEngine.Core.Window;

namespace HumbleEngine.Silk;

public class SilkWindow : HmblWindow
{
    public SilkWindow()
    {
        window = SlkWindow.Create(
            WindowOptions.Default with {
                Title = Title.Value,
            }
        );
        window.Load += EmitLoaded;
        window.Update += EmitFixUpdated;
        window.Render += EmitRendering;
        window.Closing += EmitClosing;

    }
    
    private IWindow window;
    
    public override void Run() 
        => window.Run();

    public override void Reset() 
        => window.Reset();

    protected override void SetTitle(string title) 
        => window.Title = title;
    
    
}