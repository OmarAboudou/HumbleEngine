using System.Runtime.CompilerServices;

namespace HumbleEngine;

public readonly struct ElementId : IEquatable<ElementId>
{
    private readonly object? _key;   // set when Key is explicit
    private readonly UINode? _root;  // set when path-based
    private readonly string? _path;  // set when path-based

    private ElementId(object key)                { _key = key;  _root = null; _path = null; }
    private ElementId(UINode root, string path)  { _key = null; _root = root; _path = path; }

    internal static ElementId FromKey(object key)                => new(key);
    internal static ElementId FromPath(UINode root, string path) => new(root, path);

    public bool Equals(ElementId other)
        => _key != null
            ? Equals(_key, other._key)
            : ReferenceEquals(_root, other._root) && _path == other._path;

    public override bool Equals(object? obj) => obj is ElementId id && Equals(id);

    public override int GetHashCode()
        => _key != null
            ? _key.GetHashCode()
            : HashCode.Combine(RuntimeHelpers.GetHashCode(_root), _path);

    public static bool operator ==(ElementId a, ElementId b) => a.Equals(b);
    public static bool operator !=(ElementId a, ElementId b) => !a.Equals(b);
}
