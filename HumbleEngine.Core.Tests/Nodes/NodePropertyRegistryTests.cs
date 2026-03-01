using HumbleEngine.Core;
using NUnit.Framework;

namespace HumbleEngine.Core.Tests;

// -------------------------------------------------------------------------
// Nodes de test représentant différentes configurations de membres
// -------------------------------------------------------------------------

file class BaseTestNode : Node
{
    // [Exposed] seul — visible dans l'inspecteur, non modifiable depuis une scène.
    // Compatible avec une propriété calculée sans setter.
    [Exposed]
    public float ComputedSpeed => 42f;

    // [Exposed] + [Overridable] — cas le plus courant : visible ET modifiable.
    [Exposed]
    [Overridable]
    public string DisplayName { get; private set; } = "default";

    // [Overridable] seul — modifiable depuis une scène, invisible dans l'inspecteur.
    // Cas write-only : l'éditeur permet de fixer la valeur sans la lire.
    [Overridable]
    public string WriteOnlyConfig { private get; set; } = "initial";

    // Champ [Exposed] — les champs sont aussi supportés.
    [Exposed]
    public int ExposedField = 7;

    // Champ [Overridable] — modifiable par réflexion depuis une scène.
    [Overridable]
    public float OverridableField = 1.0f;

    // Membre ordinaire — ni exposed ni overridable, invisible du système.
    public int InternalCounter { get; set; }

    // Accesseur pour vérifier la valeur de WriteOnlyConfig dans les tests.
    public string GetWriteOnlyConfig() => WriteOnlyConfig;
}

file sealed class DerivedTestNode : BaseTestNode
{
    // Propriété ajoutée dans la classe dérivée — vérifie que l'héritage fonctionne.
    [Exposed]
    [Overridable]
    public int MaxHealth { get; private set; } = 100;
}

file sealed class MisconfiguredNode : Node
{
    // [Overridable] sur propriété sans setter — erreur de configuration détectable.
    [Overridable]
    public string BadProperty => "readonly";
}

// -------------------------------------------------------------------------
// Tests
// -------------------------------------------------------------------------

[TestFixture]
public class NodePropertyRegistryTests
{
    // -------------------------------------------------------------------------
    // Détection des membres selon leur combinaison d'attributs
    // -------------------------------------------------------------------------

    [Test]
    public void GetProperties_Returns_ExposedOnlyProperty()
    {
        var props = NodePropertyRegistry.GetProperties(typeof(BaseTestNode));
        var prop = props.FirstOrDefault(p => p.Name == "ComputedSpeed");

        Assert.That(prop, Is.Not.Null);
        Assert.That(prop!.IsExposed, Is.True);
        Assert.That(prop.IsOverridable, Is.False);
    }

    [Test]
    public void GetProperties_Returns_OverridableOnlyProperty()
    {
        var props = NodePropertyRegistry.GetProperties(typeof(BaseTestNode));
        var prop = props.FirstOrDefault(p => p.Name == "WriteOnlyConfig");

        Assert.That(prop, Is.Not.Null);
        Assert.That(prop!.IsExposed, Is.False);
        Assert.That(prop.IsOverridable, Is.True);
    }

    [Test]
    public void GetProperties_Returns_ExposedAndOverridableProperty()
    {
        var props = NodePropertyRegistry.GetProperties(typeof(BaseTestNode));
        var prop = props.FirstOrDefault(p => p.Name == "DisplayName");

        Assert.That(prop, Is.Not.Null);
        Assert.That(prop!.IsExposed, Is.True);
        Assert.That(prop.IsOverridable, Is.True);
    }

    [Test]
    public void GetProperties_DoesNotReturn_UnmarkedProperty()
    {
        var props = NodePropertyRegistry.GetProperties(typeof(BaseTestNode));
        Assert.That(props.Any(p => p.Name == "InternalCounter"), Is.False);
    }

    // -------------------------------------------------------------------------
    // Support des champs
    // -------------------------------------------------------------------------

    [Test]
    public void GetProperties_Returns_ExposedField()
    {
        var props = NodePropertyRegistry.GetProperties(typeof(BaseTestNode));
        var prop = props.FirstOrDefault(p => p.Name == "ExposedField");

        Assert.That(prop, Is.Not.Null);
        Assert.That(prop!.IsExposed, Is.True);
        Assert.That(prop.IsOverridable, Is.False);
    }

    [Test]
    public void GetProperties_Returns_OverridableField()
    {
        var props = NodePropertyRegistry.GetProperties(typeof(BaseTestNode));
        var prop = props.FirstOrDefault(p => p.Name == "OverridableField");

        Assert.That(prop, Is.Not.Null);
        Assert.That(prop!.IsOverridable, Is.True);
    }

    // -------------------------------------------------------------------------
    // Héritage de membres
    // -------------------------------------------------------------------------

    [Test]
    public void GetProperties_IncludesInheritedMembers()
    {
        var props = NodePropertyRegistry.GetProperties(typeof(DerivedTestNode));

        Assert.That(props.Any(p => p.Name == "ComputedSpeed"), Is.True);
        Assert.That(props.Any(p => p.Name == "DisplayName"), Is.True);
        Assert.That(props.Any(p => p.Name == "MaxHealth"), Is.True);
    }

    // -------------------------------------------------------------------------
    // Lecture et écriture de valeurs
    // -------------------------------------------------------------------------

    [Test]
    public void GetValue_ReturnsCorrectValue_ForExposedProperty()
    {
        var node = new BaseTestNode();
        var prop = NodePropertyRegistry.GetProperties(node)
            .First(p => p.Name == "ComputedSpeed");

        Assert.That(prop.GetValue(node), Is.EqualTo(42f));
    }

    [Test]
    public void GetValue_ReturnsCorrectValue_ForExposedField()
    {
        var node = new BaseTestNode();
        var prop = NodePropertyRegistry.GetProperties(node)
            .First(p => p.Name == "ExposedField");

        Assert.That(prop.GetValue(node), Is.EqualTo(7));
    }

    [Test]
    public void GetValue_Throws_ForOverridableOnlyMember()
    {
        // Un membre [Overridable] sans [Exposed] ne peut pas être lu
        // via l'inspecteur — c'est volontairement write-only.
        var node = new BaseTestNode();
        var prop = NodePropertyRegistry.GetProperties(node)
            .First(p => p.Name == "WriteOnlyConfig");

        Assert.That(() => prop.GetValue(node), Throws.InvalidOperationException);
    }

    [Test]
    public void SetValue_WritesValue_OnOverridableProperty()
    {
        var node = new BaseTestNode();
        var prop = NodePropertyRegistry.GetProperties(node)
            .First(p => p.Name == "DisplayName");

        prop.SetValue(node, "overridden");

        Assert.That(node.DisplayName, Is.EqualTo("overridden"));
    }

    [Test]
    public void SetValue_WritesValue_OnOverridableField()
    {
        var node = new BaseTestNode();
        var prop = NodePropertyRegistry.GetProperties(node)
            .First(p => p.Name == "OverridableField");

        prop.SetValue(node, 9.5f);

        Assert.That(node.OverridableField, Is.EqualTo(9.5f));
    }

    [Test]
    public void SetValue_WritesValue_OnWriteOnlyProperty()
    {
        var node = new BaseTestNode();
        var prop = NodePropertyRegistry.GetProperties(node)
            .First(p => p.Name == "WriteOnlyConfig");

        prop.SetValue(node, "overridden");

        Assert.That(node.GetWriteOnlyConfig(), Is.EqualTo("overridden"));
    }

    [Test]
    public void SetValue_Throws_OnExposedOnlyProperty()
    {
        var node = new BaseTestNode();
        var prop = NodePropertyRegistry.GetProperties(node)
            .First(p => p.Name == "ComputedSpeed");

        Assert.That(() => prop.SetValue(node, 99f), Throws.InvalidOperationException);
    }

    // -------------------------------------------------------------------------
    // Cache — le registre ne fait la réflexion qu'une seule fois
    // -------------------------------------------------------------------------

    [Test]
    public void GetProperties_ReturnsSameInstance_OnMultipleCalls()
    {
        var first = NodePropertyRegistry.GetProperties(typeof(BaseTestNode));
        var second = NodePropertyRegistry.GetProperties(typeof(BaseTestNode));

        Assert.That(ReferenceEquals(first, second), Is.True);
    }

    // -------------------------------------------------------------------------
    // Détection des erreurs de configuration
    // -------------------------------------------------------------------------

    [Test]
    public void GetConfigurationErrors_Detects_MissingSetterOnOverridableProperty()
    {
        var errors = NodePropertyRegistry.GetConfigurationErrors(typeof(MisconfiguredNode));

        Assert.That(errors.Any(e => e.Contains("BadProperty") && e.Contains("setter")), Is.True);
    }

    [Test]
    public void GetConfigurationErrors_ReturnsEmpty_ForWellConfiguredNode()
    {
        var errors = NodePropertyRegistry.GetConfigurationErrors(typeof(BaseTestNode));
        Assert.That(errors, Is.Empty);
    }

    // -------------------------------------------------------------------------
    // Filtres utilitaires
    // -------------------------------------------------------------------------

    [Test]
    public void GetOverridableProperties_ReturnsOnlyOverridable()
    {
        var props = NodePropertyRegistry.GetOverridableProperties(typeof(BaseTestNode));

        Assert.That(props.All(p => p.IsOverridable), Is.True);
        Assert.That(props.Any(p => p.Name == "DisplayName"), Is.True);
        Assert.That(props.Any(p => p.Name == "ComputedSpeed"), Is.False);
    }

    [Test]
    public void GetExposedProperties_ReturnsOnlyExposed()
    {
        var props = NodePropertyRegistry.GetExposedProperties(typeof(BaseTestNode));

        Assert.That(props.All(p => p.IsExposed), Is.True);
        Assert.That(props.Any(p => p.Name == "ComputedSpeed"), Is.True);
        Assert.That(props.Any(p => p.Name == "WriteOnlyConfig"), Is.False);
    }
}