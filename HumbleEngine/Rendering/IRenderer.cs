namespace HumbleEngine;

public interface IRenderer : IDisposable
{
    bool Supports(GPUBackend backend);
    void Initialize(GPUBackend backend);
    void Render(PaintCommandBuffer buffer);
    void Resize(Size size);
}
