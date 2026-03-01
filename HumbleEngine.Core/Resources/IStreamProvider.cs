namespace HumbleEngine.Core.Resources;

/// <summary>
/// Contrat d'un provider capable de résoudre un chemin vers un flux de données brutes.
///
/// <para>
/// Un <see cref="IStreamProvider"/> est responsable d'un schéma d'URI donné et sait
/// comment produire un <see cref="Stream"/> à partir d'un chemin relatif à ce schéma.
/// Il n'a aucune connaissance de la nature des données transmises — JSON, binaire,
/// image, audio — c'est le consommateur qui décide comment interpréter le flux.
/// </para>
///
/// <para>
/// Le moteur fournit <see cref="FileSystemStreamProvider"/> pour le schéma <c>res://</c>.
/// Les utilisateurs peuvent enregistrer leurs propres providers pour des schémas
/// arbitraires — <c>remote://</c>, <c>pak://</c>, <c>memory://</c>, etc. —
/// sans modifier le moteur.
/// </para>
///
/// <para>
/// <b>Durée de vie du Stream</b> : le consommateur qui reçoit le stream retourné
/// par <see cref="LoadAsync"/> est responsable de le disposer (<c>await using</c>
/// ou <c>using</c> explicite).
/// </para>
/// </summary>
public interface IStreamProvider
{
    /// <summary>
    /// Charge et retourne un flux de données pour le chemin donné.
    /// </summary>
    /// <param name="path">
    /// Chemin sans le schéma. Pour l'URI <c>res://scenes/player.hscene</c>,
    /// le provider reçoit <c>scenes/player.hscene</c>.
    /// </param>
    /// <returns>
    /// Un <see cref="Stream"/> positionné au début des données.
    /// Le consommateur est responsable de le disposer.
    /// </returns>
    /// <exception cref="StreamNotFoundException">
    /// Levée si aucune donnée n'est accessible à ce chemin.
    /// </exception>
    Task<Stream> LoadAsync(string path);

    /// <summary>
    /// Indique si des données sont accessibles au chemin donné, sans les charger.
    /// </summary>
    Task<bool> ExistsAsync(string path);
}