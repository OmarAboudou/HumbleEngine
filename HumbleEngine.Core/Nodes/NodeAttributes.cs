namespace HumbleEngine.Core;

/// <summary>
/// Marque une propriété ou un champ de node comme visible dans l'éditeur,
/// en lecture seule. L'inspecteur affiche la valeur courante mais ne permet
/// pas de la modifier depuis une scène.
///
/// Exige l'existence d'un getter (propriété calculée autorisée).
/// Peut être cumulé avec [Overridable] pour rendre un membre à la fois
/// visible et modifiable depuis une scène.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field,
    Inherited = true,
    AllowMultiple = false)]
public sealed class ExposedAttribute : Attribute { }

/// <summary>
/// Marque une propriété ou un champ de node comme modifiable depuis une
/// EmbeddedScene. Le moteur écrit la valeur par réflexion à l'instanciation.
///
/// Pour les propriétés : exige l'existence d'un setter (public ou privé).
/// Pour les champs : toujours modifiable par réflexion, quel que soit
/// le modificateur d'accès.
///
/// Peut être cumulé avec [Exposed] pour rendre un membre à la fois visible
/// dans l'inspecteur et modifiable depuis une scène — c'est le cas le plus courant.
/// Sans [Exposed], le membre reste modifiable depuis une scène mais invisible
/// dans l'inspecteur (utile pour les propriétés write-only).
/// </summary>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field,
    Inherited = true,
    AllowMultiple = false)]
public sealed class OverridableAttribute : Attribute { }