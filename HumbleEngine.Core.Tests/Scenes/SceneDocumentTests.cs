using HumbleEngine.Core.Scenes;
using NUnit.Framework;

namespace HumbleEngine.Core.Tests.Scenes;

[TestFixture]
file class SceneDocumentTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static SceneNode SimpleNode(string id = "root") => new(
        Id: id,
        TypeName: "Game.MyNode",
        GenericBindings: new Dictionary<string, string>(),
        Properties: new Dictionary<string, object?>(),
        Slots: new Dictionary<string, SceneSlotDefinition>(),
        Children: Array.Empty<SceneElement>()
    );

    private static SceneDocument BaseDocument(SceneElement? root = null) => new(
        SchemaVersion: 1,
        Kind: SceneKind.Base,
        ExtendsScene: null,
        Implements: Array.Empty<string>(),
        ForceNonInstantiable: false,
        Root: root ?? SimpleNode(),
        ReplaceVirtuals: new Dictionary<string, SceneElement>(),
        FillSlots: new Dictionary<string, IReadOnlyList<SceneElement>>(),
        SetProperties: new Dictionary<string, IReadOnlyDictionary<string, object?>>()
    );

    // -------------------------------------------------------------------------
    // SceneDocument — structure de base
    // -------------------------------------------------------------------------

    [Test]
    public void SceneDocument_BaseScene_HasRoot_AndEmptyOverrideDicts()
    {
        var doc = BaseDocument();

        Assert.That(doc.Root, Is.Not.Null);
        Assert.That(doc.ReplaceVirtuals, Is.Empty);
        Assert.That(doc.FillSlots, Is.Empty);
        Assert.That(doc.SetProperties, Is.Empty);
    }

    [Test]
    public void SceneDocument_InheritedScene_HasNullRoot_AndOverrideDicts()
    {
        var replacement = SimpleNode("ctrl");
        var doc = new SceneDocument(
            SchemaVersion: 1,
            Kind: SceneKind.Inherited,
            ExtendsScene: "res://base.hscene",
            Implements: Array.Empty<string>(),
            ForceNonInstantiable: false,
            Root: null,
            ReplaceVirtuals: new Dictionary<string, SceneElement> { ["controller"] = replacement },
            FillSlots: new Dictionary<string, IReadOnlyList<SceneElement>>
            {
                ["abilities"] = new[] { SimpleNode("dash") }
            },
            SetProperties: new Dictionary<string, IReadOnlyDictionary<string, object?>>
            {
                ["player"] = new Dictionary<string, object?> { ["speed"] = 6.0 }
            }
        );

        Assert.That(doc.Root, Is.Null);
        Assert.That(doc.ReplaceVirtuals["controller"], Is.EqualTo(replacement));
        Assert.That(doc.FillSlots["abilities"], Has.Count.EqualTo(1));
        Assert.That(doc.SetProperties["player"]["speed"], Is.EqualTo(6.0));
    }

    [Test]
    public void SceneDocument_StructuralEquality_WorksCorrectly()
    {
        var doc1 = BaseDocument();
        var doc2 = BaseDocument();

        Assert.That(doc1, Is.EqualTo(doc2));
    }

    // -------------------------------------------------------------------------
    // SceneNode — slots séparés des children
    // -------------------------------------------------------------------------

    [Test]
    public void SceneNode_SlotsAndChildren_AreIndependent()
    {
        var gridNode = SimpleNode("grid");
        var slotDef = new SceneSlotDefinition(
            AcceptedType: "Game.IEntry",
            TargetNodeId: "grid",
            Visibility: SlotVisibility.Public,
            Items: Array.Empty<SceneElement>()
        );

        var node = new SceneNode(
            Id: "container",
            TypeName: "Game.ContainerNode",
            GenericBindings: new Dictionary<string, string>(),
            Properties: new Dictionary<string, object?>(),
            Slots: new Dictionary<string, SceneSlotDefinition> { ["entries"] = slotDef },
            Children: new[] { gridNode }
        );

        // Le slot et le node cible sont bien des concepts distincts.
        Assert.That(node.Slots["entries"].TargetNodeId, Is.EqualTo("grid"));
        Assert.That(node.Children[0].Id, Is.EqualTo("grid"));
        // La liste Children ne contient pas le slot lui-même.
        Assert.That(node.Children.Count, Is.EqualTo(1));
    }

    // -------------------------------------------------------------------------
    // SceneEmbeddedScene — sans TypeConstraint
    // -------------------------------------------------------------------------

    [Test]
    public void SceneEmbeddedScene_HasNoTypeConstraint()
    {
        // On vérifie que le record ne possède plus de propriété TypeConstraint.
        // Si ce test compile, c'est que le type est bien défini sans cette propriété.
        var es = new SceneEmbeddedScene(
            Id: "weapon",
            ScenePath: "res://scenes/sword.hscene",
            GenericBindings: new Dictionary<string, string>(),
            PropertyOverrides: new Dictionary<string, object?> { ["display_name"] = "Épée" },
            SlotOverrides: new Dictionary<string, IReadOnlyList<SceneElement>>()
        );

        Assert.That(es.ScenePath, Is.EqualTo("res://scenes/sword.hscene"));
        Assert.That(es.PropertyOverrides["display_name"], Is.EqualTo("Épée"));
    }

    // -------------------------------------------------------------------------
    // SceneLoadResult
    // -------------------------------------------------------------------------

    [Test]
    public void SceneLoadResult_CanInstantiate_TrueOnlyForInstantiable()
    {
        var instantiable = new SceneLoadResult(
            BaseDocument(), SceneInstantiabilityStatus.Instantiable, Array.Empty<SceneDiagnostic>());
        var forced = new SceneLoadResult(
            BaseDocument(), SceneInstantiabilityStatus.NonInstantiableForced, Array.Empty<SceneDiagnostic>());

        Assert.That(instantiable.CanInstantiate, Is.True);
        Assert.That(forced.CanInstantiate, Is.False);
    }

    [Test]
    public void SceneLoadResult_HasErrors_TrueWhenErrorDiagnosticPresent()
    {
        var error = new SceneDiagnostic("SCN0002", SceneDiagnosticSeverity.Error, "Clé manquante");
        var result = new SceneLoadResult(null, SceneInstantiabilityStatus.Invalid, new[] { error });

        Assert.That(result.HasErrors, Is.True);
    }

    [Test]
    public void SceneLoadResult_HasErrors_FalseWithOnlyInfoDiagnostics()
    {
        var info = new SceneDiagnostic("SCN0019", SceneDiagnosticSeverity.Info, "NonInstantiable");
        var result = new SceneLoadResult(
            BaseDocument(), SceneInstantiabilityStatus.NonInstantiableByStructure, new[] { info });

        Assert.That(result.HasErrors, Is.False);
    }
}