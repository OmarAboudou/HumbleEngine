namespace HumbleEngine.Core.Scenes;

// =============================================================================
// Énumérations
// =============================================================================

/// <summary>
/// Détermine si une scène est une scène de base ou hérite d'une autre scène.
/// </summary>
public enum SceneKind { Base, Inherited }

/// <summary>
/// Statut d'instanciabilité d'une scène, déterminé après validation complète.
/// Seul <see cref="Instantiable"/> permet d'appeler Instantiate().
/// </summary>
public enum SceneInstantiabilityStatus
{
    /// <summary>Structure valide, tous les éléments concrets — instanciable.</summary>
    Instantiable,

    /// <summary>force_non_instantiable = true — chargeable et héritable, non instanciable.</summary>
    NonInstantiableForced,

    /// <summary>Structure invalide (types abstraits, required manquants…) — chargeable et héritable.</summary>
    NonInstantiableByStructure,

    /// <summary>Incohérence de données (JSON illisible, contraintes violées) — mode réparation.</summary>
    Invalid,
}

/// <summary>
/// Visibilité d'un slot : public (assignable par la scène englobante et les héritières)
/// ou protected (assignable uniquement par les héritières).
/// </summary>
public enum SlotVisibility { Public, Protected }

// =============================================================================
// Éléments structurels — hiérarchie de records
// =============================================================================

/// <summary>
/// Élément de base de l'arborescence d'une scène.
/// Tous les éléments partagent un identifiant unique dans la scène.
/// </summary>
/// <param name="Id">Identifiant unique de l'élément dans la scène.</param>
public abstract record SceneElement(string Id);

/// <summary>
/// Node concret, fixe — son type ne peut pas être remplacé par une héritière.
/// Ses slots sont déclarés séparément de ses enfants structurels.
/// </summary>
/// <param name="Id">Identifiant unique de ce node dans la scène.</param>
/// <param name="TypeName">Nom de type C# qualifié (ex: "Game.PlayerNode`1").</param>
/// <param name="GenericBindings">
/// Fermeture des paramètres génériques du type de ce node.
/// Clé = nom du paramètre (ex: "TStats"), valeur = type C# qualifié.
/// </param>
/// <param name="Properties">
/// Valeurs des propriétés [Overridable] à appliquer à l'instanciation.
/// Clé = nom de propriété en snake_case, valeur = valeur JSON brute.
/// </param>
/// <param name="Slots">
/// Slots exposés par ce node. Clé = id du slot, valeur = définition du slot.
/// Séparé des Children — un slot n'est pas un enfant structurel du node.
/// </param>
/// <param name="Children">
/// Enfants directs de ce node dans l'arborescence : autres nodes, virtual nodes,
/// et embedded scenes. Ne contient jamais de slots.
/// </param>
public sealed record SceneNode(
    string Id,
    string TypeName,
    IReadOnlyDictionary<string, string> GenericBindings,
    IReadOnlyDictionary<string, object?> Properties,
    IReadOnlyDictionary<string, SceneSlotDefinition> Slots,
    IReadOnlyList<SceneElement> Children
) : SceneElement(Id);

/// <summary>
/// Définition d'un slot déclaré sur un node.
/// </summary>
/// <param name="AcceptedType">Type contraint des éléments injectables (nom de type C# qualifié).</param>
/// <param name="TargetNodeId">Id du node interne vers lequel les enfants sont effectivement ajoutés.</param>
/// <param name="Visibility">Visibilité du slot — détermine qui peut y injecter des éléments.</param>
/// <param name="Items">Éléments déjà injectés dans ce slot.</param>
public sealed record SceneSlotDefinition(
    string AcceptedType,
    string TargetNodeId,
    SlotVisibility Visibility,
    IReadOnlyList<SceneElement> Items
);

/// <summary>
/// Emplacement overridable par une scène héritière — analogue à une méthode virtual/abstract.
/// Sans default et non required, l'emplacement reste vide à l'instanciation.
/// </summary>
/// <param name="Id">Identifiant unique de ce node virtuel dans la scène.</param>
/// <param name="TypeConstraint">Type contraint du node attendu. Peut être un paramètre générique de la racine.</param>
/// <param name="Required">
/// Si true, la scène est NonInstantiable tant qu'aucune héritière ne fournit ce node.
/// Analogue à abstract.
/// </param>
/// <param name="Default">
/// Valeur par défaut optionnelle : un node concret ou une EmbeddedScene compatible.
/// Null si absent.
/// </param>
public sealed record SceneVirtualNode(
    string Id,
    string TypeConstraint,
    bool Required,
    SceneElement? Default
) : SceneElement(Id);

/// <summary>
/// Référence à une scène externe, utilisée là où un node est attendu.
/// Peut surcharger les propriétés et slots publics de la scène référencée.
/// La compatibilité de type est vérifiée par le validateur via le système de types C# —
/// aucune contrainte de type explicite n'est nécessaire dans le JSON.
/// </summary>
/// <param name="Id">Identifiant unique de cette référence dans la scène.</param>
/// <param name="ScenePath">Chemin vers le fichier .hscene référencé (ex: "res://scenes/sword.hscene").</param>
/// <param name="GenericBindings">Fermeture de paramètres génériques de la scène référencée.</param>
/// <param name="PropertyOverrides">Overrides de propriétés [Overridable] de la scène référencée.</param>
/// <param name="SlotOverrides">
/// Remplissage des slots publics de la scène référencée.
/// Clé = id du slot, valeur = éléments à injecter.
/// </param>
public sealed record SceneEmbeddedScene(
    string Id,
    string ScenePath,
    IReadOnlyDictionary<string, string> GenericBindings,
    IReadOnlyDictionary<string, object?> PropertyOverrides,
    IReadOnlyDictionary<string, IReadOnlyList<SceneElement>> SlotOverrides
) : SceneElement(Id);

// =============================================================================
// Document racine
// =============================================================================

/// <summary>
/// Représentation en mémoire d'un fichier .hscene après parsing.
/// Immuable après construction.
///
/// Pour une BaseScene, <see cref="Root"/> est non null et les trois dictionnaires
/// d'overrides sont vides. Pour une InheritedScene, <see cref="Root"/> est null
/// et les trois dictionnaires portent les modifications à appliquer sur la structure héritée.
/// </summary>
/// <param name="SchemaVersion">Version du schéma JSON (actuellement 1).</param>
/// <param name="Kind">Indique si la scène est une BaseScene ou une InheritedScene.</param>
/// <param name="ExtendsScene">
/// Chemin vers la scène parente pour une InheritedScene. Null pour une BaseScene.
/// </param>
/// <param name="Implements">Noms des contrats implémentés par cette scène.</param>
/// <param name="ForceNonInstantiable">
/// Si true, la scène est NonInstantiableForced même si sa structure est valide.
/// </param>
/// <param name="Root">Node racine pour une BaseScene. Null pour une InheritedScene.</param>
/// <param name="ReplaceVirtuals">
/// Remplacement de NodeVirtuel hérités.
/// Clé = id du NodeVirtuel ciblé, valeur = élément remplaçant.
/// </param>
/// <param name="FillSlots">
/// Injection dans des slots hérités.
/// Clé = id du slot ciblé, valeur = éléments à injecter.
/// </param>
/// <param name="SetProperties">
/// Modification de propriétés [Overridable] sur des nodes hérités.
/// Clé = id du node ciblé, valeur = dictionnaire (nom de propriété → valeur).
/// </param>
public sealed record SceneDocument(
    int SchemaVersion,
    SceneKind Kind,
    string? ExtendsScene,
    IReadOnlyList<string> Implements,
    bool ForceNonInstantiable,
    SceneElement? Root,
    IReadOnlyDictionary<string, SceneElement> ReplaceVirtuals,
    IReadOnlyDictionary<string, IReadOnlyList<SceneElement>> FillSlots,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> SetProperties
);