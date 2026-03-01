namespace HumbleEngine.Core.Resources;

/// <summary>
/// Provider de streams qui résout les chemins vers le système de fichiers local.
/// Fournit l'implémentation de référence pour le schéma <c>res://</c>.
///
/// <para>
/// Tous les chemins sont résolus relativement à la <c>rootPath</c> fournie au
/// constructeur. Ainsi, <c>res://scenes/player.hscene</c> avec une racine
/// <c>/home/user/mygame</c> ouvrira le fichier
/// <c>/home/user/mygame/scenes/player.hscene</c>.
/// </para>
/// </summary>
public sealed class FileSystemStreamProvider : IStreamProvider
{
    private readonly string _rootPath;

    /// <param name="rootPath">
    /// Chemin absolu vers la racine du projet. Tous les chemins sont résolus
    /// relativement à ce dossier.
    /// </param>
    public FileSystemStreamProvider(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("La racine du projet ne peut pas être vide.", nameof(rootPath));

        _rootPath = Path.GetFullPath(rootPath);
    }

    /// <inheritdoc/>
    public Task<Stream> LoadAsync(string path)
    {
        var fullPath = ResolvePath(path);

        if (!File.Exists(fullPath))
            throw new StreamNotFoundException($"res://{path}",
                $"fichier introuvable à '{fullPath}'");

        try
        {
            // File.OpenRead retourne un FileStream — on l'enveloppe dans Task.FromResult
            // parce que l'ouverture elle-même est synchrone sur le filesystem local.
            // La lecture du stream par le consommateur peut se faire de façon async
            // via ReadAsync/CopyToAsync si nécessaire.
            Stream stream = File.OpenRead(fullPath);
            return Task.FromResult(stream);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new StreamNotFoundException($"res://{path}",
                $"impossible d'ouvrir le fichier '{fullPath}'", ex);
        }
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string path)
    {
        try
        {
            return Task.FromResult(File.Exists(ResolvePath(path)));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Résout un chemin relatif vers un chemin absolu sous la racine du projet.
    /// Rejette les tentatives de path traversal (<c>../../etc/passwd</c>).
    /// </summary>
    private string ResolvePath(string path)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, path));

        if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
            throw new StreamNotFoundException($"res://{path}",
                "le chemin tente de sortir de la racine du projet (path traversal interdit)");

        return fullPath;
    }
}