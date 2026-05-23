namespace HumbleEngine;

public interface ITypeface
{
    string FamilyName { get; }
    bool   IsBold     { get; }
    bool   IsItalic   { get; }
}
