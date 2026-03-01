namespace HumbleEngine.Core;

/// <summary>
/// Conteneur et contrôleur d'un arbre de nodes runtime.
/// NodeTree est responsable de :
///   - l'attachement et le détachement des nodes
///   - l'injection de la référence Tree sur chaque node
///   - la file d'opérations différées (modifications pendant un tick)
///   - l'orchestration du cycle de vie
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
    /// Injecte la référence Tree sur toute la hiérarchie existante du node,
    /// puis déclenche le cycle de vie complet (Entering → Entered → Ready).
    /// </summary>
    public void SetRoot(Node root)
    {
        if (Root is not null)
            throw new InvalidOperationException(
                "Cet arbre a déjà une racine. Appelez ClearRoot() d'abord.");

        Root = root;
        InjectTree(root);

        // Propagation du cycle de vie sur toute la hiérarchie existante.
        root.PropagateTreeEntering();
        root.PropagateTreeEntered();
        root.PropagateReady();
    }

    // -------------------------------------------------------------------------
    // File d'opérations différées
    // -------------------------------------------------------------------------

    // On représente chaque opération en attente comme une simple Action.
    // C'est suffisant pour l'instant — on affinera la représentation
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

    /// <summary>
    /// Avance d'un tick logique. Applique d'abord les changements structurels
    /// en attente, puis propage OnProcess sur tout l'arbre.
    /// </summary>
    public void Process(float delta)
    {
        FlushPendingChanges();
        Root?.PropagateProcess(delta);
    }

    /// <summary>
    /// Avance d'un tick physique. Applique d'abord les changements structurels
    /// en attente, puis propage OnPhysicsProcess sur tout l'arbre.
    /// </summary>
    public void PhysicsProcess(float delta)
    {
        FlushPendingChanges();
        Root?.PropagatePhysicsProcess(delta);
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

            // Propagation du cycle de vie dans l'ordre défini par la spec :
            // 1. OnTreeEntering — parent → enfants
            // 2. OnTreeEntered  — enfants → parent
            // 3. OnReady        — enfants → parent (une seule fois par node)
            child.PropagateTreeEntering();
            child.PropagateTreeEntered();
            child.PropagateReady();
        });
    }

    internal void EnqueueRemoveChild(Node parent, Node child)
    {
        _pendingOperations.Enqueue(() =>
        {
            // OnTreeExiting est appelé AVANT de retirer Tree — le node
            // peut encore accéder à Tree dans ce callback.
            child.PropagateTreeExiting();

            WithdrawTree(child);
            parent.DetachChild(child);

            // OnTreeExited est appelé APRÈS le retrait — Tree est null.
            child.PropagateTreeExited();
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