namespace HumbleEngine;

/// <summary>
/// Shared state of one editing session — injected into the editor's panels
/// (hierarchy, inspector). Reactive: one panel writes the selection, the others
/// react to it, with no direct coupling between them. The single source of truth
/// for "what is being edited right now".
/// </summary>
public sealed class EditorState
{
    /// <summary>
    /// The currently selected node, or null. The hierarchy panel sets it on click;
    /// the inspector (bloc 3) will bind to it. Single selection for now — a
    /// multi-selection <c>ObservableList&lt;Node&gt;</c> is the noted evolution.
    /// </summary>
    public Property<Node?> Selection { get; } = new(null);
}
