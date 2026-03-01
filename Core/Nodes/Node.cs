namespace HumbleEngine.Core;

/// <summary>
/// Unité fondamentale de composition runtime dans HumbleEngine.
/// Un Node est une entité vivante dans un NodeTree — il ne représente
/// pas un concept sérialisé.
/// </summary>
public abstract class Node
{
    // -------------------------------------------------------------------------
    // Identité
    // -------------------------------------------------------------------------

    /// <summary>
    /// Identité unique du node dans le runtime.
    /// Immuable pour toute la durée de vie du node.
    /// </summary>
    public Guid Id { get; }

    // -------------------------------------------------------------------------
    // Constructeurs
    // -------------------------------------------------------------------------

    /// <summary>
    /// Crée un node avec un Guid généré automatiquement.
    /// Cas nominal pour tout usage applicatif.
    /// </summary>
    protected Node()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Crée un node avec un Guid explicite.
    /// Réservé aux usages internes : tests déterministes, import de données.
    /// </summary>
    protected Node(Guid id)
    {
        Id = id;
    }

    // -------------------------------------------------------------------------
    // Structure — parent / enfants
    // -------------------------------------------------------------------------

    /// <summary>
    /// Parent direct de ce node. Null si le node est une racine ou détaché.
    /// </summary>
    public Node? Parent { get; private set; }

    // La liste interne des enfants. On utilise List<Node> pour préserver
    // l'ordre, qui est un invariant garanti de la spec.
    private readonly List<Node> _children = new();

    /// <summary>
    /// Enfants directs de ce node, dans leur ordre d'insertion.
    /// </summary>
    public IReadOnlyList<Node> Children => _children;

    // -------------------------------------------------------------------------
    // Référence Tree
    // -------------------------------------------------------------------------

    /// <summary>
    /// Le NodeTree auquel ce node est attaché.
    /// Null tant que le node n'est pas dans un NodeTree.
    /// Cette référence est injectée par NodeTree lui-même — jamais écrite
    /// directement par le node.
    /// </summary>
    public NodeTree? Tree { get; private set; }

    /// <summary>
    /// Indique si ce node est actuellement attaché à un NodeTree.
    /// </summary>
    public bool IsInsideTree => Tree is not null;

    // Méthode interne réservée à NodeTree pour injecter/retirer la référence.
    // Le modificateur 'internal' garantit que seul le moteur peut l'appeler.
    internal void SetTree(NodeTree? tree) => Tree = tree;

    // -------------------------------------------------------------------------
    // API structurelle — AddChild / RemoveChild
    // -------------------------------------------------------------------------

    /// <summary>
    /// Ajoute un node enfant à la fin de la liste des enfants.
    /// - Hors NodeTree : modification immédiate.
    /// - Dans un NodeTree : opération différée, appliquée au prochain flush.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Si l'enfant a déjà un parent, ou appartient à un NodeTree différent.
    /// </exception>
    public void AddChild(Node child)
    {
        ValidateAddChild(child);

        if (Tree is not null)
            // On délègue au NodeTree qui gère la file d'opérations différées.
            Tree.EnqueueAddChild(this, child, index: null);
        else
            // Hors tree : application immédiate.
            AttachChild(child, index: null);
    }

    /// <summary>
    /// Ajoute un node enfant à un index précis dans la liste des enfants.
    /// </summary>
    public void AddChild(Node child, int index)
    {
        ValidateAddChild(child);

        if (Tree is not null)
            Tree.EnqueueAddChild(this, child, index);
        else
            AttachChild(child, index);
    }

    /// <summary>
    /// Retire un node enfant de ce node.
    /// - Hors NodeTree : modification immédiate.
    /// - Dans un NodeTree : opération différée, appliquée au prochain flush.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Si le node fourni n'est pas un enfant direct de ce node.
    /// </exception>
    public void RemoveChild(Node child)
    {
        if (child.Parent != this)
            throw new InvalidOperationException(
                $"Le node '{child.Id}' n'est pas un enfant direct de '{Id}'.");

        if (Tree is not null)
            Tree.EnqueueRemoveChild(this, child);
        else
            DetachChild(child);
    }

    // -------------------------------------------------------------------------
    // Méthodes internes — manipulation directe de la structure
    // Appelées par NodeTree lors du flush, ou immédiatement hors tree.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Attache physiquement un enfant à ce node.
    /// Appelé par NodeTree lors du flush des opérations différées,
    /// ou directement si le node est hors tree.
    /// </summary>
    internal void AttachChild(Node child, int? index)
    {
        child.Parent = this;

        if (index.HasValue)
            _children.Insert(index.Value, child);
        else
            _children.Add(child);
    }

    /// <summary>
    /// Détache physiquement un enfant de ce node.
    /// </summary>
    internal void DetachChild(Node child)
    {
        _children.Remove(child);
        child.Parent = null;
    }

    // -------------------------------------------------------------------------
    // Validation
    // -------------------------------------------------------------------------

    private void ValidateAddChild(Node child)
    {
        if (child is null)
            throw new ArgumentNullException(nameof(child));

        if (child.Parent is not null)
            throw new InvalidOperationException(
                $"Le node '{child.Id}' a déjà un parent ('{child.Parent.Id}'). " +
                "Retirez-le de son parent actuel avant de l'ajouter.");

        // Si l'enfant est déjà dans un NodeTree différent, il doit d'abord
        // en être retiré explicitement. On ne transfère pas implicitement —
        // l'ancien tree doit rester cohérent.
        if (child.Tree is not null && child.Tree != Tree)
            throw new InvalidOperationException(
                $"Le node '{child.Id}' appartient à un autre NodeTree. " +
                "Retirez-le de son arbre actuel via RemoveChild avant de l'ajouter ici.");

        // Empêche les cycles : on vérifie que 'child' n'est pas un ancêtre de 'this'.
        if (IsAncestorOf(child))
            throw new InvalidOperationException(
                $"Impossible d'ajouter '{child.Id}' : créerait un cycle dans l'arbre.");
    }

    /// <summary>
    /// Vérifie si ce node est un ancêtre du node fourni.
    /// Utilisé pour détecter les cycles avant un AddChild.
    /// </summary>
    private bool IsAncestorOf(Node candidate)
    {
        var current = Parent;
        while (current is not null)
        {
            if (current == candidate) return true;
            current = current.Parent;
        }
        return false;
    }

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    public override string ToString() => $"{GetType().Name}({Id})";
}