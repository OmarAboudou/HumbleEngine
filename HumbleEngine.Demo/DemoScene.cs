using HumbleEngine;
using HumbleEngine.Demo.UI;

namespace HumbleEngine.Demo;

public class DemoScene : Node
{
    public DemoScene()
    {
        this.Attach(new DashboardNode());
    }
}
