namespace HumbleEngine;

/// <summary>
/// The hierarchy panel: a live, top-to-bottom list of a root's children as
/// <see cref="HierarchyRow"/>s, each recursing into its own subtree. Mirrors
/// <paramref name="root"/>'s <see cref="HumbleEngine.Node.Children"/> through
/// <c>BindItemsFrom</c> — the panel follows the real tree as it changes.
/// <para>
/// The panel's own <see cref="UINode.Size"/> is set by the editor layout (it is a
/// docked, eventually scrollable region); only the rows self-measure, so the inner
/// <c>Column</c> can stack them. Click-to-select arrives in the next block.
/// </para>
/// </summary>
public sealed class HierarchyView : UINode
{
    private readonly Column _list;

    /// <summary>Builds the panel mirroring <paramref name="root"/>'s children, selecting into <paramref name="editor"/>.</summary>
    public HierarchyView(Node root, EditorState editor)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(editor);
        _list = new Column();
        Attach(_list);
        _list.Children.BindItemsFrom(root.Children, node => new HierarchyRow(node, editor));
    }
}
