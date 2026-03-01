namespace HumbleEngine.Core.Resources;

/// <summary>
/// Registre central des providers de streams, indexés par schéma d'URI.
///
/// <para>
/// Le <see cref="StreamSystem"/> est le point d'entrée unique pour accéder à
/// n'importe quel flux de données dans Humble. Il extrait le schéma de l'URI
/// demandée et délègue intégralement la résolution au <see cref="IStreamProvider"/>
/// correspondant. Il n'a aucune connaissance des schémas eux-mêmes.
/// </para>
///
/// <para>
/// Usage typique :
/// <code>
/// var streams = new StreamSystem();
/// streams.Register("res", new FileSystemStreamProvider("/path/to/project"));
/// streams.Register("remote", new HttpStreamProvider("https://cdn.example.com"));
///
/// await using var stream = await streams.LoadAsync("res://scenes/player.hscene");
/// </code>
/// </para>
/// </summary>
public sealed class StreamSystem
{
    private readonly Dictionary<string, IStreamProvider> _providers = new();
    private readonly object _lock = new();

    /// <summary>
    /// Enregistre un provider pour un schéma donné.
    /// Si un provider était déjà enregistré pour ce schéma, il est remplacé.
    /// </summary>
    /// <param name="scheme">
    /// Schéma sans le <c>://</c> — ex : <c>"res"</c>, <c>"remote"</c>, <c>"pak"</c>.
    /// La comparaison est insensible à la casse.
    /// </param>
    public void Register(string scheme, IStreamProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (string.IsNullOrWhiteSpace(scheme))
            throw new ArgumentException("Le schéma ne peut pas être vide.", nameof(scheme));

        lock (_lock)
            _providers[scheme.ToLowerInvariant()] = provider;
    }

    /// <summary>
    /// Ouvre un flux de données pour l'URI donnée.
    /// </summary>
    /// <param name="uri">URI au format <c>schéma://chemin</c>.</param>
    /// <returns>
    /// Un <see cref="Stream"/> positionné au début des données.
    /// Le consommateur est responsable de le disposer.
    /// </returns>
    /// <exception cref="StreamNotFoundException">
    /// Levée si le schéma n'est pas enregistré ou si le provider
    /// ne trouve pas de données au chemin demandé.
    /// </exception>
    public async Task<Stream> LoadAsync(string uri)
    {
        var (scheme, path) = ParseUri(uri);
        var provider = FindProvider(scheme, uri);

        try
        {
            return await provider.LoadAsync(path).ConfigureAwait(false);
        }
        catch (StreamNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new StreamNotFoundException(uri,
                $"le provider '{scheme}' a levé une exception inattendue", ex);
        }
    }

    /// <summary>
    /// Indique si des données sont accessibles à l'URI donnée, sans les charger.
    /// Retourne <c>false</c> si le schéma n'est pas enregistré.
    /// </summary>
    public async Task<bool> ExistsAsync(string uri)
    {
        try
        {
            var (scheme, path) = ParseUri(uri);

            IStreamProvider? provider;
            lock (_lock)
                _providers.TryGetValue(scheme.ToLowerInvariant(), out provider);

            return provider is not null && await provider.ExistsAsync(path).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    // -------------------------------------------------------------------------
    // Helpers privés
    // -------------------------------------------------------------------------

    /// <summary>
    /// Décompose une URI en schéma et chemin.
    /// <c>"res://scenes/player.hscene"</c> → <c>("res", "scenes/player.hscene")</c>
    /// </summary>
    private static (string scheme, string path) ParseUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("L'URI ne peut pas être vide.", nameof(uri));

        var separatorIndex = uri.IndexOf("://", StringComparison.Ordinal);

        if (separatorIndex <= 0)
            throw new StreamNotFoundException(uri,
                "l'URI n'est pas au format attendu 'schéma://chemin'");

        var scheme = uri[..separatorIndex].ToLowerInvariant();
        var path   = uri[(separatorIndex + 3)..];

        if (string.IsNullOrEmpty(path))
            throw new StreamNotFoundException(uri, "le chemin ne peut pas être vide");

        return (scheme, path);
    }

    private IStreamProvider FindProvider(string scheme, string originalUri)
    {
        lock (_lock)
        {
            if (_providers.TryGetValue(scheme, out var provider))
                return provider;
        }

        throw new StreamNotFoundException(originalUri,
            $"aucun provider n'est enregistré pour le schéma '{scheme}'. " +
            $"Appelez StreamSystem.Register(\"{scheme}\", ...) avant de charger cette ressource.");
    }
}