namespace HumbleEngine.Core.Scenes;

/// <summary>
/// Valide un <see cref="SceneDocument"/> après parsing et détermine son statut
/// d'instanciabilité définitif.
///
/// La validation s'organise en deux passes séquentielles :
/// - Passe 1 (structurelle) : vérifie la cohérence interne du document
///   sans nécessiter de contexte extérieur (ids uniques, cibles valides…).
///   Une erreur ici produit le statut <see cref="SceneInstantiabilityStatus.Invalid"/>.
/// - Passe 2 (instanciabilité) : applique les conditions de la section 4.1
///   de la spec. Une erreur ici produit <see cref="SceneInstantiabilityStatus.NonInstantiableByStructure"/>
///   sauf si <see cref="SceneDocument.ForceNonInstantiable"/> est true.
///
/// La résolution de type C# (génériques, compatibilité d'héritage) n'est pas
/// encore implémentée et fera l'objet d'une étape ultérieure.
/// </summary>
public sealed class SceneValidator
{
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
        // On collecte tous les ids déclarés dans le document pour détecter
        // les doublons. On parcourt l'arbre en profondeur et on mémorise
        // chaque id rencontré dans un HashSet — le premier doublon trouvé
        // déclenche SCN0015.
        var seenIds = new HashSet<string>();

        if (document.Kind == SceneKind.Base && document.Root is not null)
        {
            CollectAndValidateIds(document.Root, seenIds, ctx);
        }
        else if (document.Kind == SceneKind.Inherited)
        {
            // Pour une InheritedScene, on valide les ids des éléments déclarés
            // dans les overrides — les ids hérités ne sont pas re-déclarés ici.
            foreach (var (_, element) in document.ReplaceVirtuals)
                CollectAndValidateIds(element, seenIds, ctx);

            foreach (var (_, items) in document.FillSlots)
                foreach (var item in items)
                    CollectAndValidateIds(item, seenIds, ctx);
        }
    }

    /// <summary>
    /// Parcourt récursivement un élément et ses enfants pour collecter les ids
    /// et détecter les doublons.
    /// </summary>
    private static void CollectAndValidateIds(
        SceneElement element, HashSet<string> seenIds, ValidationContext ctx)
    {
        if (!seenIds.Add(element.Id))
        {
            ctx.Error("SCN0015",
                $"Id '{element.Id}' dupliqué dans la scène.",
                elementId: element.Id);
        }

        // On descend récursivement dans les enfants selon le type d'élément.
        // Le pattern matching exhaustif garantit qu'on ne rate aucun cas
        // si de nouveaux types sont ajoutés à la hiérarchie.
        switch (element)
        {
            case SceneNode node:
                foreach (var child in node.Children)
                    CollectAndValidateIds(child, seenIds, ctx);
                // Les items des slots sont aussi des éléments de l'arbre
                // et doivent avoir des ids uniques.
                foreach (var (_, slot) in node.Slots)
                    foreach (var item in slot.Items)
                        CollectAndValidateIds(item, seenIds, ctx);
                break;

            case SceneVirtualNode vn:
                if (vn.Default is not null)
                    CollectAndValidateIds(vn.Default, seenIds, ctx);
                break;

            // SceneEmbeddedScene n'a pas d'enfants propres dans l'arbre
            // de la scène courante — ses overrides référencent des éléments
            // de la scène externe, pas de nouveaux éléments locaux.
            case SceneEmbeddedScene:
                break;
        }
    }

    // =========================================================================
    // Passe 2 — Validation d'instanciabilité
    // =========================================================================

    private static void ValidateInstantiability(SceneDocument document, ValidationContext ctx)
    {
        // Pour une BaseScene, on peut vérifier directement les NodeVirtuel
        // required qui n'ont pas de valeur par défaut — ils rendent la scène
        // non instanciable sans qu'une héritière les fournisse.
        if (document.Kind == SceneKind.Base && document.Root is not null)
            CheckRequiredVirtuals(document.Root, document, ctx);

        // Pour une InheritedScene, les replace_virtuals fournis satisfont
        // potentiellement des NodeVirtuel required hérités. Cette vérification
        // complète nécessite de charger la scène parente, ce qui sera implémenté
        // lors de l'étape de résolution de dépendances inter-scènes.
        // Pour l'instant, on valide uniquement les éléments locaux.
    }

    /// <summary>
    /// Parcourt l'arbre à la recherche de <see cref="SceneVirtualNode"/> marqués
    /// <c>required</c> sans valeur par défaut. Un tel node rend la scène
    /// <see cref="SceneInstantiabilityStatus.NonInstantiableByStructure"/>.
    /// </summary>
    private static void CheckRequiredVirtuals(
        SceneElement element, SceneDocument document, ValidationContext ctx)
    {
        if (element is SceneVirtualNode { Required: true, Default: null } vn)
        {
            // On vérifie si une InheritedScene (qui serait la scène courante)
            // fournit déjà ce virtual. Ici on traite le cas BaseScene — donc
            // un NodeVirtuel required sans default est toujours non satisfait.
            ctx.Error("SCN0020",
                $"Le NodeVirtuel '{vn.Id}' est marqué 'required' mais n'a pas de valeur par défaut. " +
                "Une scène héritière doit le fournir avant instanciation.",
                elementId: vn.Id);
        }

        // Descente récursive dans les enfants.
        if (element is SceneNode node)
        {
            foreach (var child in node.Children)
                CheckRequiredVirtuals(child, document, ctx);

            foreach (var (_, slot) in node.Slots)
                foreach (var item in slot.Items)
                    CheckRequiredVirtuals(item, document, ctx);
        }

        if (element is SceneVirtualNode { Default: not null } vnWithDefault)
            CheckRequiredVirtuals(vnWithDefault.Default, document, ctx);
    }
}

// =============================================================================
// ValidationContext — accumulation des diagnostics pendant la validation
// =============================================================================

/// <summary>
/// Contexte mutable partagé par toutes les fonctions de validation.
/// Similaire à <c>ParseContext</c> mais orienté validation sémantique —
/// les diagnostics produits ici ont souvent un <c>ElementId</c> plutôt
/// qu'un <c>JsonPath</c>, car on travaille sur le modèle objet, pas sur le JSON.
/// </summary>
internal sealed class ValidationContext
{
    private readonly List<SceneDiagnostic> _diagnostics;

    public ValidationContext(IReadOnlyList<SceneDiagnostic> initialDiagnostics)
    {
        // On commence avec les diagnostics du parser pour ne pas les perdre.
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