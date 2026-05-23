using HumbleEngine;
using Silk.NET.Core.Contexts;

namespace HumbleEngine.Silk;

public class SilkGraphicsContext : IGraphicsContext
{
    private readonly IGLContext _ctx;

    public SilkGraphicsContext(IGLContext ctx)
    {
        _ctx = ctx;
    }

    public nint GetProcAddress(string name)
    {
        _ctx.TryGetProcAddress(name, out var addr);
        return addr;
    }
}
