namespace HumbleEngine;

/// <summary>
/// A node that takes part in rendering: once per frame, while its tree is
/// rendered, <see cref="OnDraw"/> receives the renderer and submits draws.
/// <para>
/// The base <see cref="Node"/> stays free of rendering vocabulary — only visual
/// nodes know the word "draw", the same specialization rule that keeps
/// transforms out of the base node. Pure-logic nodes cannot even express it.
/// </para>
/// </summary>
public abstract class VisualNode : Node
{
    /// <summary>
    /// Renderer of this node's rendering context — the <b>single access point</b>
    /// of the framework protocol, so nodes never hard-code the resolution:
    /// today it is the tree's renderer; the WindowNode/viewport evolution will
    /// resolve the nearest viewport ancestor instead, without touching any
    /// node. Null when detached: acquire GPU resources in
    /// <see cref="Node.OnAttached"/>, release them in <see cref="Node.OnDetached"/>.
    /// </summary>
    protected IRenderer? Renderer => Tree?.Renderer;

    /// <summary>
    /// Called once per frame between the renderer's <see cref="IRenderer.BeginFrame"/>
    /// and <see cref="IRenderer.EndFrame"/>, parents before children (painter's
    /// order: children draw over their parent). Submit this node's draws here.
    /// Never mutate the tree structure from a draw — destruction goes through
    /// <see cref="Node.QueueDispose"/>, flushed at end of frame.
    /// </summary>
    protected virtual void OnDraw(IRenderer renderer)
    {
    }

    /// <summary>Traversal entry point — same internal machinery as the lifecycle hooks.</summary>
    internal void Draw(IRenderer renderer) => OnDraw(renderer);
}
