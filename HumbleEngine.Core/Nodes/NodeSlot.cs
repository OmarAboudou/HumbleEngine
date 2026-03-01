namespace HumbleEngine.Core;

/// <summary>
/// Point d'insertion nommé et typé exposé par un node.
/// Injecter un node dans un slot revient à l'ajouter comme enfant
/// du node cible interne associé à ce slot.
///
/// NodeSlot&lt;T&gt; ne doit jamais être instancié directement — il est
/// toujours obtenu via Node.GetSlot&lt;T&gt;(targetNode).
/// </summary>
/// <typeparam name="T">Type contraint des nodes injectables dans ce slot.</typeparam>
public sealed class NodeSlot<T> where T : Node
{
    // Le node cible vers lequel les enfants sont effectivement redirigés.
    // C'est un nœud interne de la scène — il est distinct du node qui
    // expose le slot.
    private readonly Node _target;

    internal NodeSlot(Node target)
    {
        _target = target;
    }

    /// <summary>
    /// Node interne vers lequel les enfants injectés dans ce slot sont ajoutés.
    /// </summary>
    public Node Target => _target;

    /// <summary>
    /// Nodes actuellement injectés dans ce slot, dans leur ordre d'insertion.
    /// Ce sont les enfants directs du node cible dont le type est compatible avec T.
    /// </summary>
    public IEnumerable<T> Items => _target.Children.OfType<T>();

    /// <summary>
    /// Injecte un node dans ce slot, c'est-à-dire l'ajoute comme enfant
    /// du node cible interne.
    /// </summary>
    public void Add(T node) => _target.AddChild(node);

    /// <summary>
    /// Injecte un node à un index précis dans ce slot.
    /// </summary>
    public void Add(T node, int index) => _target.AddChild(node, index);

    /// <summary>
    /// Retire un node précédemment injecté dans ce slot.
    /// </summary>
    public void Remove(T node) => _target.RemoveChild(node);

    /// <summary>
    /// Indique si ce slot contient au moins un node.
    /// </summary>
    public bool HasItems => _target.Children.OfType<T>().Any();
}