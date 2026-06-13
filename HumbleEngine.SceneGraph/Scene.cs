namespace HumbleEngine;

/// <summary>
/// A node encapsulating a composed subtree behind a typed contract.
/// <para>
/// <b>The class is the template</b>: there is no reified scene asset — <c>new</c>
/// is the instantiation, fully typed. The interior is built declaratively in the
/// constructor (one-shot: built once, then mutated — never redescribed); its
/// <b>editing</b> surface is the scene's <b>slots</b> — typed properties
/// (interfaces welcome) delegating to private <see cref="NodeSlot{TChild}"/> /
/// <see cref="NodeList{TChild}"/>, whose assignment transfers ownership. The
/// interior is readable (like every node, through <see cref="Node.Children"/>, for
/// introspection) but not writable from the outside.
/// </para>
/// <para>
/// Scene adds no mechanics over <see cref="Node"/> — composition is already
/// closed to writing. It marks the "component" altitude: containers expose a
/// mutable composition, scenes keep theirs private (read-only from outside), and
/// the editor will one day generate partial classes of this very shape.
/// </para>
/// </summary>
public abstract class Scene : Node
{
}
