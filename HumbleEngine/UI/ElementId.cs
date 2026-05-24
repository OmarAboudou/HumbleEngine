namespace HumbleEngine;

public readonly struct ElementId : IEquatable<ElementId>
{
    private readonly object? _key;    // set for keyed elements (they ARE an anchor)
    private readonly object? _anchor; // set for path-based elements
    private readonly string? _path;   // set for path-based elements

    private ElementId(object key)                  { _key = key;  _anchor = null; _path = null; }
    private ElementId(object anchor, string path)  { _key = null; _anchor = anchor; _path = path; }

    internal static ElementId FromKey(object key)                 => new(key);
    internal static ElementId FromPath(object anchor, string path) => new(anchor, path);

    public bool Equals(ElementId other)
        => _key != null
            ? Equals(_key, other._key)
            : Equals(_anchor, other._anchor) && _path == other._path;

    public override bool Equals(object? obj) => obj is ElementId id && Equals(id);

    public override int GetHashCode()
        => _key != null
            ? _key.GetHashCode()
            : HashCode.Combine(_anchor?.GetHashCode() ?? 0, _path);

    public static bool operator ==(ElementId a, ElementId b) => a.Equals(b);
    public static bool operator !=(ElementId a, ElementId b) => !a.Equals(b);
}
