namespace HumbleEngine.Core.Scenes;

/// <summary>
/// Valide un <see cref="SceneDocument"/> après parsing et détermine son statut
/// d'instanciabilité définitif.
///
/// La validation s'organise en trois passes séquentielles :
///
/// - Passe 1 (structurelle) : vérifie la cohérence interne du document
///   sans nécessiter de contexte extérieur (ids uniques, cibles valides…).
///   Une erreur ici produit le statut <see cref="SceneInstantiabilityStatus.Invalid"/>.
///
/// - Passe 2 (instanciabilité) : applique les conditions de la section 4.1
///   de la spec (NodeVirtuel required sans default…).
///   Une erreur ici produit <see cref="SceneInstantiabilityStatus.NonInstantiableByStructure"/>.
///
/// - Passe 3 (types C#) : optionnelle — s'exécute uniquement si un
///   <see cref="TypeResolver"/> a été fourni au constructeur. Vérifie que les
///   types référencés dans le document existent, sont concrets, et que les
///   génériques sont correctement fermés.
///   Une erreur ici produit également <see cref="SceneInstantiabilityStatus.NonInstantiableByStructure"/>.
///
/// La validation inter-scènes (compatibilité d'une EmbeddedScene avec son contexte,
/// NodeVirtuel required satisfaits par une héritière) fera l'objet d'une étape ultérieure.
/// </summary>
public sealed class SceneValidator
{
    private readonly TypeResolver? _typeResolver;

    /// <summary>
    /// Crée un validateur sans résolution de types.
    /// Les passes 1 et 2 s'exécutent normalement ; la passe 3 est ignorée.
    /// Utile pour les tests unitaires qui n'ont pas besoin de réflexion C#.
    /// </summary>
    public SceneValidator() : this(null) { }

    /// <summary>
    /// Crée un validateur avec résolution de types.
    /// Les trois passes s'exécutent, y compris la vérification de l'abstraction,
    /// de l'arité générique et des contraintes génériques.
    /// </summary>
    public SceneValidator(TypeResolver? typeResolver)
    {
        _typeResolver = typeResolver;
    }

    public SceneLoadResult Validate(SceneDocument document, IReadOnlyList<SceneDiagnostic> parserDiagnostics)
    {
        var ctx = new ValidationContext(parserDiagnostics);

        // Passe 1 — validation structurelle.
        // Si le parser a déjà détecté des erreurs, on court-circuite :
        // un document partiellement parsé peut manquer des données nécessaires
        // à la validation structurelle et produire des faux positifs.
        if (ctx.HasErrors)
            return ctx.BuildResult(document, SceneInstantiabilityStatus.Invalid);

        ValidateStructure(document, ctx);

        if (ctx.HasErrors)
            return ctx.BuildResult(document, SceneInstantiabilityStatus.Invalid);

        // Passe 2 — validation d'instanciabilité.
        // Le flag force_non_instantiable prend la priorité sur tout le reste.
        if (document.ForceNonInstantiable)
            return ctx.BuildResult(document, SceneInstantiabilityStatus.NonInstantiableForced);

        ValidateInstantiability(document, ctx);

        // Passe 3 — validation des types C# (optionnelle).
        // On ne l'exécute que si un TypeResolver a été fourni. Elle s'enchaîne
        // directement à la passe 2 : ses erreurs contribuent au même statut final.
        if (_typeResolver is not null)
            ValidateTypes(document, _typeResolver, ctx);

        var status = ctx.HasErrors
            ? SceneInstantiabilityStatus.NonInstantiableByStructure
            : SceneInstantiabilityStatus.Instantiable;

        // SCN0019 est un diagnostic informatif ajouté lorsque la scène est
        // NonInstantiableByStructure — il sert à l'éditeur pour distinguer
        // cet état du statut Invalid.
        if (status == SceneInstantiabilityStatus.NonInstantiableByStructure)
            ctx.Info("SCN0019", "La scène n'est pas instanciable en raison de sa structure.");

        return ctx.BuildResult(document, status);
    }

    // =========================================================================
    // Passe 1 — Validation structurelle
    // =========================================================================

    private static void ValidateStructure(SceneDocument document, ValidationContext ctx)
    {
        var seenIds = new HashSet<string>();

        if (document.Kind == SceneKind.Base && document.Root is not null)
        {
            CollectAndValidateIds(document.Root, seenIds, ctx);
        }
        else if (document.Kind == SceneKind.Inherited)
        {
            foreach (var (_, element) in document.ReplaceVirtuals)
                CollectAndValidateIds(element, seenIds, ctx);

            foreach (var (_, items) in document.FillSlots)
                foreach (var item in items)
                    CollectAndValidateIds(item, seenIds, ctx);
        }
    }

    private static void CollectAndValidateIds(
        SceneElement element, HashSet<string> seenIds, ValidationContext ctx)
    {
        if (!seenIds.Add(element.Id))
        {
            ctx.Error("SCN0015",
                $"Id '{element.Id}' dupliqué dans la scène.",
                elementId: element.Id);
        }

        switch (element)
        {
            case SceneNode node:
                foreach (var child in node.Children)
                    CollectAndValidateIds(child, seenIds, ctx);
                foreach (var (_, slot) in node.Slots)
                    foreach (var item in slot.Items)
                        CollectAndValidateIds(item, seenIds, ctx);
                break;

            case SceneVirtualNode vn:
                if (vn.Default is not null)
                    CollectAndValidateIds(vn.Default, seenIds, ctx);
                break;

            case SceneEmbeddedScene:
                break;
        }
    }

    // =========================================================================
    // Passe 2 — Validation d'instanciabilité
    // =========================================================================

    private static void ValidateInstantiability(SceneDocument document, ValidationContext ctx)
    {
        if (document.Kind == SceneKind.Base && document.Root is not null)
            CheckRequiredVirtuals(document.Root, ctx);

        // Pour une InheritedScene, la vérification complète des NodeVirtuel required
        // nécessite de charger la scène parente — reporté à l'étape inter-scènes.
    }

    private static void CheckRequiredVirtuals(SceneElement element, ValidationContext ctx)
    {
        if (element is SceneVirtualNode { Required: true, Default: null } vn)
        {
            ctx.Error("SCN0020",
                $"Le NodeVirtuel '{vn.Id}' est marqué 'required' mais n'a pas de valeur par défaut. " +
                "Une scène héritière doit le fournir avant instanciation.",
                elementId: vn.Id);
        }

        if (element is SceneNode node)
        {
            foreach (var child in node.Children)
                CheckRequiredVirtuals(child, ctx);

            foreach (var (_, slot) in node.Slots)
                foreach (var item in slot.Items)
                    CheckRequiredVirtuals(item, ctx);
        }

        if (element is SceneVirtualNode { Default: not null } vnWithDefault)
            CheckRequiredVirtuals(vnWithDefault.Default, ctx);
    }

    // =========================================================================
    // Passe 3 — Validation des types C#
    // =========================================================================

    private static void ValidateTypes(SceneDocument document, TypeResolver resolver, ValidationContext ctx)
    {
        if (document.Kind == SceneKind.Base && document.Root is not null)
            ValidateElementTypes(document.Root, resolver, ctx);
        else if (document.Kind == SceneKind.Inherited)
        {
            foreach (var (_, element) in document.ReplaceVirtuals)
                ValidateElementTypes(element, resolver, ctx);

            foreach (var (_, items) in document.FillSlots)
                foreach (var item in items)
                    ValidateElementTypes(item, resolver, ctx);
        }
    }

    /// <summary>
    /// Valide les types d'un élément et descend récursivement dans ses enfants.
    /// Seuls les <see cref="SceneNode"/> font l'objet d'une validation de type —
    /// les <see cref="SceneVirtualNode"/> ont un <c>type_constraint</c> qui peut
    /// être un paramètre générique ouvert (non résolvable sans contexte), et les
    /// <see cref="SceneEmbeddedScene"/> sont vérifiées en compatibilité inter-scènes.
    /// </summary>
    private static void ValidateElementTypes(SceneElement element, TypeResolver resolver, ValidationContext ctx)
    {
        if (element is SceneNode node)
        {
            ValidateNodeType(node, resolver, ctx);

            foreach (var child in node.Children)
                ValidateElementTypes(child, resolver, ctx);

            foreach (var (_, slot) in node.Slots)
                foreach (var item in slot.Items)
                    ValidateElementTypes(item, resolver, ctx);
        }

        // Descente dans le default d'un NodeVirtuel — le default est un node
        // concret ou une EmbeddedScene, dont les types sont valides à vérifier.
        if (element is SceneVirtualNode { Default: not null } vn)
            ValidateElementTypes(vn.Default, resolver, ctx);
    }

    /// <summary>
    /// Valide le type d'un <see cref="SceneNode"/> :
    /// résolution, abstraction (SCN0008), et violations de contraintes génériques (SCN0011).
    ///
    /// <para>
    /// <b>SCN0008 — type abstrait</b> : un type abstrait (classe abstraite ou interface)
    /// produit <see cref="SceneInstantiabilityStatus.NonInstantiableByStructure"/>, ce qui
    /// signifie que la scène est chargeable et éditable, mais pas directement instanciable.
    /// C'est le comportement attendu pour les scènes de base abstraites qui servent de
    /// fondation à des scènes héritières concrètes.
    /// </para>
    ///
    /// <para>
    /// <b>SCN0012 — type générique ouvert sans bindings</b> : ce cas n'est PAS traité ici.
    /// Un node de type générique ouvert peut être légitime lorsque ses paramètres correspondent
    /// aux paramètres génériques de la scène racine — ils seront alors fournis à l'appel de
    /// <c>Instantiate(GenericTypeArguments)</c>. La validation de SCN0012 est reportée à
    /// l'étape d'instanciation, où les arguments effectifs sont disponibles.
    /// </para>
    /// </summary>
    private static void ValidateNodeType(SceneNode node, TypeResolver resolver, ValidationContext ctx)
    {
        // On résout le type simple du node — sans appliquer les generic_bindings pour
        // l'instant, car leur ordre dans le dictionnaire ne correspond pas nécessairement
        // à l'ordre des paramètres génériques du type C#. Le mapping correct
        // (clé du binding → paramètre générique par nom) sera implémenté lors de
        // l'intégration avec l'instanciateur, qui aura accès à type.GetGenericArguments().
        var result = resolver.Resolve(TypeRef.Simple(node.TypeName));

        switch (result)
        {
            case TypeResolveResult.TypeNotFound:
                // On ne produit pas d'erreur : un type introuvable peut être un type
                // utilisateur dont l'assembly n'est pas encore enregistré dans l'éditeur.
                // Bloquer ici forcerait l'enregistrement des assemblies avant même de
                // pouvoir ouvrir une scène — une expérience trop contraignante.
                // Ce comportement sera affiné quand le cycle de vie des assemblies sera défini.
                break;

            case TypeResolveResult.ConstraintViolation violation:
                // SCN0011 : un binding viole une contrainte générique déclarée sur le type.
                ctx.Error("SCN0011",
                    $"Contrainte générique non satisfaite sur '{node.TypeName}' : {violation.Constraint}.",
                    elementId: node.Id);
                break;

            case TypeResolveResult.GenericArityMismatch:
                // Un type générique ouvert résolu sans arguments — cas normal pour une scène
                // générique dont les paramètres seront fournis à l'instanciation. Pas d'erreur.
                break;

            case TypeResolveResult.Success success:
                // SCN0008 : le type est abstrait. La scène sera NonInstantiableByStructure
                // mais reste chargeable et héritable — c'est le comportement attendu.
                if (success.Type.IsAbstract)
                    ctx.Error("SCN0008",
                        $"Le type '{node.TypeName}' est abstrait et ne peut pas être instancié directement. " +
                        "La scène peut servir de base à des scènes héritières concrètes.",
                        elementId: node.Id);
                break;
        }
    }
}

// =============================================================================
// ValidationContext
// =============================================================================

/// <summary>
/// Contexte mutable partagé par toutes les fonctions de validation.
/// Accumule les diagnostics des trois passes dans une liste unique.
/// </summary>
internal sealed class ValidationContext
{
    private readonly List<SceneDiagnostic> _diagnostics;

    public ValidationContext(IReadOnlyList<SceneDiagnostic> initialDiagnostics)
    {
        _diagnostics = new List<SceneDiagnostic>(initialDiagnostics);
    }

    public bool HasErrors => _diagnostics.Any(d => d.Severity == SceneDiagnosticSeverity.Error);

    public void Error(string code, string message, string? elementId = null) =>
        _diagnostics.Add(new SceneDiagnostic(
            code, SceneDiagnosticSeverity.Error, message, ElementId: elementId));

    public void Info(string code, string message) =>
        _diagnostics.Add(new SceneDiagnostic(code, SceneDiagnosticSeverity.Info, message));

    public SceneLoadResult BuildResult(SceneDocument document, SceneInstantiabilityStatus status) =>
        new(document, status, _diagnostics);
}