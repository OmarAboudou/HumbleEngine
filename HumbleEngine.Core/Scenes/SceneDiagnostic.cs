namespace HumbleEngine.Core.Scenes;

// =============================================================================
// Diagnostics
// =============================================================================

public enum SceneDiagnosticSeverity { Error, Warning, Info }

/// <summary>
/// Diagnostic produit lors du chargement ou de la validation d'une scène.
/// Un diagnostic transporte le code d'erreur, la sévérité, un message lisible,
/// et des métadonnées optionnelles pour aider à la réparation dans l'éditeur.
/// </summary>
/// <param name="Code">Code d'erreur unique (ex: "SCN0002").</param>
/// <param name="Severity">Sévérité du diagnostic.</param>
/// <param name="Message">Message lisible décrivant le problème.</param>
/// <param name="JsonPath">Chemin JSON vers l'élément problématique (ex: "root.children[2].properties").</param>
/// <param name="ElementId">Id de l'élément de scène concerné, si applicable.</param>
/// <param name="Suggestion">Suggestion de correction affichée dans l'éditeur.</param>
/// <param name="CanAutoRepair">Indique si l'éditeur peut proposer une réparation automatique.</param>
public sealed record SceneDiagnostic(
    string Code,
    SceneDiagnosticSeverity Severity,
    string Message,
    string? JsonPath = null,
    string? ElementId = null,
    string? Suggestion = null,
    bool CanAutoRepair = false
);

// =============================================================================
// Résultat de chargement
// =============================================================================

/// <summary>
/// Résultat complet du chargement d'un fichier .hscene.
/// Regroupe le document parsé (null si JSON illisible), le statut
/// d'instanciabilité, et la liste des diagnostics produits.
/// </summary>
/// <remarks>
/// Un résultat avec <see cref="Status"/> = <see cref="SceneInstantiabilityStatus.Invalid"/>
/// peut avoir un <see cref="Document"/> null.
/// Un résultat avec Status = NonInstantiable* a toujours un Document non null.
/// </remarks>
/// <param name="Document">Document parsé, ou null si le JSON était illisible.</param>
/// <param name="Status">Statut d'instanciabilité déterminé après validation.</param>
/// <param name="Diagnostics">Liste des diagnostics produits durant le chargement.</param>
public sealed record SceneLoadResult(
    SceneDocument? Document,
    SceneInstantiabilityStatus Status,
    IReadOnlyList<SceneDiagnostic> Diagnostics
)
{
    /// <summary>Indique si la scène peut être instanciée.</summary>
    public bool CanInstantiate => Status == SceneInstantiabilityStatus.Instantiable;

    /// <summary>Indique si des erreurs ont été détectées (toute sévérité).</summary>
    public bool HasErrors => Diagnostics.Any(d => d.Severity == SceneDiagnosticSeverity.Error);
}