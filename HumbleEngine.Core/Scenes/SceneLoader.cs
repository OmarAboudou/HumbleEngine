namespace HumbleEngine.Core.Scenes;

/// <summary>
/// Façade principale pour le chargement de scènes.
/// Orchestre <see cref="SceneParser"/> et <see cref="SceneValidator"/> et retourne
/// toujours un <see cref="SceneLoadResult"/> complet — y compris lorsque le JSON
/// est illisible ou que la scène est invalide.
///
/// <para>
/// C'est le consommateur qui décide quoi faire du résultat selon son contexte :
/// un éditeur inspectera les diagnostics pour entrer en mode réparation, tandis
/// qu'un runtime pourra lever une exception si le statut est
/// <see cref="SceneInstantiabilityStatus.Invalid"/>.
/// </para>
///
/// <para>
/// Pour activer la validation des types C# (SCN0008, SCN0011, SCN0012), fournir
/// un <see cref="TypeResolver"/> préalablement configuré avec les assemblies du projet.
/// Sans resolver, les passes 1 et 2 s'exécutent normalement et la passe 3 est ignorée.
/// </para>
/// </summary>
public sealed class SceneLoader
{
    private readonly SceneParser _parser;
    private readonly SceneValidator _validator;

    /// <summary>Crée un loader sans validation de types.</summary>
    public SceneLoader() : this(new SceneParser(), new SceneValidator()) { }

    /// <summary>Crée un loader avec validation de types C#.</summary>
    public SceneLoader(TypeResolver typeResolver)
        : this(new SceneParser(), new SceneValidator(typeResolver)) { }

    /// <summary>Constructeur complet pour les tests et l'injection de dépendances.</summary>
    public SceneLoader(SceneParser parser, SceneValidator validator)
    {
        _parser = parser;
        _validator = validator;
    }

    /// <summary>
    /// Charge une scène depuis une chaîne JSON.
    /// Retourne toujours un <see cref="SceneLoadResult"/>, même si le document est
    /// invalide ou non instanciable. Les diagnostics du parser et du validator
    /// sont agrégés dans <see cref="SceneLoadResult.Diagnostics"/>.
    ///
    /// <para>
    /// Si le JSON est totalement illisible, <see cref="SceneLoadResult.Document"/>
    /// est <c>null</c> et le statut est <see cref="SceneInstantiabilityStatus.Invalid"/>.
    /// La validation est alors court-circuitée — le validator n'a rien à valider.
    /// </para>
    /// </summary>
    public SceneLoadResult Load(string json)
    {
        var parseResult = _parser.Parse(json);

        if (parseResult.Document is null)
            return parseResult;

        return _validator.Validate(parseResult.Document, parseResult.Diagnostics);
    }
}