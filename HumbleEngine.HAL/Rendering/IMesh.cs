namespace HumbleEngine;

/// <summary>
/// Opaque handle to GPU-resident geometry, created by
/// <see cref="IRenderer.CreateMesh"/> and drawable through
/// <see cref="IRenderer.Draw"/> on the renderer that created it.
/// Owned by the caller: dispose it before its renderer — the usual reverse
/// creation order.
/// </summary>
public interface IMesh : IDisposable;
