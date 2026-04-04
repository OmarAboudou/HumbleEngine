namespace HumbleEngine.Core;

/// <summary>
/// Marque une classe comme type sérialisable et lui associe un identifiant stable sous forme de GUID.
/// Le GUID doit être unique dans toute l'application et ne doit jamais changer une fois assigné,
/// car il est utilisé pour la sérialisation persistante.
/// </summary>
/// <param name="id">Le GUID sous forme de chaîne (ex : "a1b2c3d4-e5f6-7890-abcd-ef1234567890").</param>
[AttributeUsage(AttributeTargets.Class,  AllowMultiple = false, Inherited = false)]
public class HumbleTypeAttribute(string id) : Attribute
{
    /// <summary>
    /// Le GUID associé à ce type, sous forme de chaîne.
    /// </summary>
    public string Id { get; }= id;
}