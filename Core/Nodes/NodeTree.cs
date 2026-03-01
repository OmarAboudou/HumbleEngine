namespace HumbleEngine.Core;

/// <summary>
/// Conteneur et contrôleur d'un arbre de nodes runtime.
/// NodeTree est responsable de :
///   - l'attachement et le détachement des nodes
///   - l'injection de la référence Tree sur chaque node
///   - la file d'opérations différées (modifications pendant un tick)
///   - l'orchestration du cycle de vie (étape 3)
/// </summary>
public sealed class NodeTree
{
    // -------------------------------------------------------------------------
    // Racine
    // -------------------------------------------------------------------------

    /// <summary>
    /// Node racine de cet arbre. Null si l'arbre est vide.
    /// </summary>
    public Node? Root { get; private set; }

    /// <summary>
    /// Définit le node racine de cet arbre.
    /// Injecte la référence Tree sur toute la hiérarchie existante du node.
    /// </summary>
    public void SetRoot(Node root)
    {
        if (Root is not null)
            throw new InvalidOperationException(
                "Cet arbre a déjà une racine. Appelez ClearRoot() d'abord.");

        Root = root;
        InjectTree(root);
    }

    // -------------------------------------------------------------------------
    // File d'opérations différées
    // -------------------------------------------------------------------------

    // On représente chaque opération en attente comme une simple Action.
    // C'est suffisant pour l'étape 1 — on affinera la représentation
    // (type discriminé, log rejouable) dans une phase ultérieure.
    private readonly Queue<Action> _pendingOperations = new();

    /// <summary>
    /// Applique toutes les opérations structurelles en attente.
    /// Appelé automatiquement par Process() et PhysicsProcess()
    /// avant leurs callbacks respectifs.
    /// </summary>
    public void FlushPendingChanges()
    {
        while (_pendingOperations.TryDequeue(out var operation))
            operation();
    }

    // -------------------------------------------------------------------------
    // Enqueue — appelés par Node.AddChild / Node.RemoveChild
    // -------------------------------------------------------------------------

    internal void EnqueueAddChild(Node parent, Node child, int? index)
    {
        _pendingOperations.Enqueue(() =>
        {
            parent.AttachChild(child, index);
            InjectTree(child);
        });
    }

    internal void EnqueueRemoveChild(Node parent, Node child)
    {
        _pendingOperations.Enqueue(() =>
        {
            WithdrawTree(child);
            parent.DetachChild(child);
        });
    }

    // -------------------------------------------------------------------------
    // Injection / retrait de la référence Tree
    // -------------------------------------------------------------------------

    // On propage Tree en profondeur d'abord (parent avant enfants),
    // ce qui correspond à l'ordre naturel d'entrée dans l'arbre.
    private void InjectTree(Node node)
    {
        node.SetTree(this);
        foreach (var child in node.Children)
            InjectTree(child);
    }

    // On retire Tree en remontant (enfants avant parent),
    // cohérent avec l'ordre de détachement défini dans la spec.
    private void WithdrawTree(Node node)
    {
        foreach (var child in node.Children)
            WithdrawTree(child);
        node.SetTree(null);
    }
}