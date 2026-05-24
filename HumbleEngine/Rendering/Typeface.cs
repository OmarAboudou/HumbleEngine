namespace HumbleEngine;

public readonly struct Typeface
{
    public string FamilyName { get; }
    public bool   IsBold     { get; }
    public bool   IsItalic   { get; }

    public Typeface(string familyName, bool bold = false, bool italic = false)
    {
        FamilyName = familyName;
        IsBold     = bold;
        IsItalic   = italic;
    }
}
