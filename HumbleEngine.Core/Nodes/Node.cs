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
    // Cycle de vie
    // -------------------------------------------------------------------------

    // OnReady n'est invoqué qu'une seule fois par node, même si le node
    // est retiré puis réattaché à un arbre. Ce flag le garantit.
    private bool _readyInvoked;

    /// <summary>
    /// Appelé au début de l'entrée dans le NodeTree.
    /// Propagation : parent → enfants.
    /// À ce stade, les enfants ne sont pas encore entrés.
    /// </summary>
    protected virtual void OnTreeEntering() { }

    /// <summary>
    /// Appelé après que tout le sous-arbre est entré dans le NodeTree.
    /// Propagation : enfants → parent.
    /// À ce stade, tous les enfants ont déjà reçu OnTreeEntering.
    /// </summary>
    protected virtual void OnTreeEntered() { }

    /// <summary>
    /// Appelé une seule fois après OnTreeEntered, lors du premier attachement.
    /// Propagation : enfants → parent.
    /// Garantie : quand OnReady s'exécute sur ce node, tous ses enfants
    /// ont déjà reçu leur propre OnReady.
    /// </summary>
    protected virtual void OnReady() { }

    /// <summary>
    /// Appelé à chaque tick logique.
    /// </summary>
    /// <param name="delta">Temps écoulé depuis le dernier tick, en secondes.</param>
    protected virtual void OnProcess(float delta) { }

    /// <summary>
    /// Appelé à chaque tick physique.
    /// </summary>
    /// <param name="delta">Temps écoulé depuis le dernier tick physique, en secondes.</param>
    protected virtual void OnPhysicsProcess(float delta) { }

    /// <summary>
    /// Appelé avant la sortie effective du NodeTree.
    /// Propagation : enfants → parent.
    /// Tree est encore accessible à ce stade.
    /// </summary>
    protected virtual void OnTreeExiting() { }

    /// <summary>
    /// Appelé après la sortie effective du NodeTree.
    /// Propagation : enfants → parent.
    /// Tree est null à ce stade.
    /// </summary>
    protected virtual void OnTreeExited() { }

    // Méthodes internes appelées par NodeTree pour orchestrer le cycle de vie.
    // On les sépare des callbacks virtuels pour garder le contrôle
    // sur l'ordre exact de propagation — NodeTree ne doit pas avoir
    // à connaître les détails de propagation, il délègue à Node.

    internal void PropagateTreeEntering()
    {
        // Parent d'abord, puis enfants — l'onde descend.
        OnTreeEntering();
        foreach (var child in _children)
            child.PropagateTreeEntering();
    }

    internal void PropagateTreeEntered()
    {
        // Enfants d'abord, puis parent — l'onde remonte.
        foreach (var child in _children)
            child.PropagateTreeEntered();
        OnTreeEntered();
    }

    internal void PropagateReady()
    {
        // Enfants d'abord, puis parent — même logique que OnTreeEntered.
        foreach (var child in _children)
            child.PropagateReady();

        // OnReady n'est invoqué qu'une seule fois, même après un
        // retrait et réattachement ultérieur.
        if (!_readyInvoked)
        {
            _readyInvoked = true;
            OnReady();
        }
    }

    internal void PropagateProcess(float delta)
    {
        // Process n'a pas d'ordre de propagation défini dans la spec —
        // on choisit parent → enfants par convention, cohérent avec
        // l'ordre naturel de mise à jour (le parent peut affecter ses enfants).
        OnProcess(delta);
        foreach (var child in _children)
            child.PropagateProcess(delta);
    }

    internal void PropagatePhysicsProcess(float delta)
    {
        OnPhysicsProcess(delta);
        foreach (var child in _children)
            child.PropagatePhysicsProcess(delta);
    }

    internal void PropagateTreeExiting()
    {
        // Enfants d'abord, puis parent — l'onde remonte.
        // Tree est encore accessible ici (retiré après OnTreeExited).
        foreach (var child in _children)
            child.PropagateTreeExiting();
        OnTreeExiting();
    }

    internal void PropagateTreeExited()
    {
        // Enfants d'abord, puis parent.
        foreach (var child in _children)
            child.PropagateTreeExited();
        OnTreeExited();
    }

    // -------------------------------------------------------------------------
    // Slots
    // -------------------------------------------------------------------------

    // Cache des instances NodeSlot<T> déjà créées pour ce node.
    // La clé est le node cible — chaque node cible ne peut avoir qu'un seul
    // slot associé sur un node donné. On utilise un Dictionary<Node, object>
    // parce que NodeSlot<T> est générique et qu'on ne peut pas stocker
    // des types génériques ouverts dans un Dictionary typé.
    private Dictionary<Node, object>? _slotCache;

    /// <summary>
    /// Résout ou crée le NodeSlot&lt;T&gt; pointant vers le node cible donné.
    /// Le résultat est mis en cache — deux appels successifs avec le même
    /// node cible retournent toujours la même instance.
    ///
    /// À appeler depuis les propriétés [Slot] des nodes concrets :
    /// <code>
    /// [Slot]
    /// public NodeSlot&lt;InventoryEntry&gt; Entries => GetSlot&lt;InventoryEntry&gt;(_grid);
    /// </code>
    /// </summary>
    protected NodeSlot<T> GetSlot<T>(Node target) where T : Node
    {
        _slotCache ??= new Dictionary<Node, object>();

        if (_slotCache.TryGetValue(target, out var cached))
            return (NodeSlot<T>)cached;

        var slot = new NodeSlot<T>(target);
        _slotCache[target] = slot;
        return slot;
    }

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    public override string ToString() => $"{GetType().Name}({Id})";
}