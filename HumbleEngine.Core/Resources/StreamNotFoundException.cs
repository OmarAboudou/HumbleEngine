namespace HumbleEngine.Core.Resources;

/// <summary>
/// Levée lorsqu'un chemin ne peut pas être résolu vers un flux de données,
/// que ce soit parce que le schéma n'est pas enregistré dans le
/// <see cref="StreamSystem"/> ou parce que le provider ne trouve pas
/// de données au chemin demandé.
/// </summary>
public sealed class StreamNotFoundException : Exception
{
    /// <summary>URI complète qui n'a pas pu être résolue.</summary>
    public string Uri { get; }

    public StreamNotFoundException(string uri, string reason)
        : base($"Stream introuvable pour '{uri}' : {reason}")
    {
        Uri = uri;
    }

    public StreamNotFoundException(string uri, string reason, Exception inner)
        : base($"Stream introuvable pour '{uri}' : {reason}", inner)
    {
        Uri = uri;
    }
}