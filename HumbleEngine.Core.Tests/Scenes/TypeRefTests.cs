using HumbleEngine.Core.Scenes;
using NUnit.Framework;

namespace HumbleEngine.Core.Tests.Scenes;

[TestFixture]
internal sealed class TypeRefTests
{
    // -------------------------------------------------------------------------
    // Construction et propriétés de base
    // -------------------------------------------------------------------------

    [Test]
    public void Simple_CreatesTypeRef_WithEmptyArgs()
    {
        var typeRef = TypeRef.Simple("Game.Sword");

        Assert.That(typeRef.TypeName, Is.EqualTo("Game.Sword"));
        Assert.That(typeRef.Args, Is.Empty);
        Assert.That(typeRef.IsGeneric, Is.False);
    }

    [Test]
    public void IsGeneric_TrueWhenArgsNonEmpty()
    {
        var typeRef = new TypeRef("Game.Inventory`1", new[] { TypeRef.Simple("Game.Sword") });

        Assert.That(typeRef.IsGeneric, Is.True);
    }

    // -------------------------------------------------------------------------
    // ToString — lisibilité pour diagnostics
    // -------------------------------------------------------------------------

    [Test]
    public void ToString_SimpleType_ReturnsTypeName()
    {
        Assert.That(TypeRef.Simple("Game.Sword").ToString(), Is.EqualTo("Game.Sword"));
    }

    [Test]
    public void ToString_GenericType_ReturnsCSharpLikeNotation()
    {
        var typeRef = new TypeRef("Game.Inventory`1", new[] { TypeRef.Simple("Game.Sword") });

        Assert.That(typeRef.ToString(), Is.EqualTo("Game.Inventory`1<Game.Sword>"));
    }

    [Test]
    public void ToString_DeepGeneric_IsRecursive()
    {
        // Inventory<List<Sword>>
        var inner = new TypeRef("System.Collections.Generic.List`1", new[] { TypeRef.Simple("Game.Sword") });
        var outer = new TypeRef("Game.Inventory`1", new[] { inner });

        Assert.That(outer.ToString(), Is.EqualTo("Game.Inventory`1<System.Collections.Generic.List`1<Game.Sword>>"));
    }

    // -------------------------------------------------------------------------
    // Égalité structurelle (héritée des records)
    // -------------------------------------------------------------------------

    [Test]
    public void StructuralEquality_SameContent_AreEqual()
    {
        var a = new TypeRef("Game.Inventory`1", new[] { TypeRef.Simple("Game.Sword") });
        var b = new TypeRef("Game.Inventory`1", new[] { TypeRef.Simple("Game.Sword") });

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void StructuralEquality_DifferentArgs_AreNotEqual()
    {
        var a = new TypeRef("Game.Inventory`1", new[] { TypeRef.Simple("Game.Sword") });
        var b = new TypeRef("Game.Inventory`1", new[] { TypeRef.Simple("Game.Shield") });

        Assert.That(a, Is.Not.EqualTo(b));
    }
}