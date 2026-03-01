using HumbleEngine.Core.Scenes;
using NUnit.Framework;

namespace HumbleEngine.Core.Tests.Scenes;

// Ces tests vérifient trois choses fondamentales sur le modèle de données :
// 1. La construction est correcte (les valeurs passées sont bien accessibles).
// 2. L'égalité structurelle des records fonctionne comme attendu.
// 3. Les propriétés dérivées (CanInstantiate, HasErrors) se comportent correctement.
//
// On ne teste pas encore le parsing JSON ni la validation — ce sont les étapes
// suivantes. Ici on pose uniquement les fondations du modèle.

[TestFixture]
file class SceneDocumentTests
{
    // -------------------------------------------------------------------------
    // Helpers — construction de documents de test
    // -------------------------------------------------------------------------

    private static SceneNode SimpleNode(string id = "root") => new(
        Id: id,
        TypeName: "Game.MyNode",
        GenericBindings: new Dictionary<string, string>(),
        Properties: new Dictionary<string, object?>(),
        Children: Array.Empty<SceneElement>()
    );

    private static SceneDocument SimpleDocument(SceneElement? root = null) => new(
        SchemaVersion: 1,
        Kind: SceneKind.Base,
        ExtendsScene: null,
        Implements: Array.Empty<string>(),
        GenericBindings: new Dictionary<string, string>(),
        ForceNonInstantiable: false,
        Root: root ?? SimpleNode()
    );

    // -------------------------------------------------------------------------
    // SceneDocument
    // -------------------------------------------------------------------------

    [Test]
    public void SceneDocument_StoresAllFields_Correctly()
    {
        var root = SimpleNode();
        var doc = new SceneDocument(
            SchemaVersion: 1,
            Kind: SceneKind.Inherited,
            ExtendsScene: "res://base.hscene",
            Implements: new[] { "Game.IClickable" },
            GenericBindings: new Dictionary<string, string> { ["T"] = "Game.Foo" },
            ForceNonInstantiable: true,
            Root: root
        );

        Assert.That(doc.SchemaVersion, Is.EqualTo(1));
        Assert.That(doc.Kind, Is.EqualTo(SceneKind.Inherited));
        Assert.That(doc.ExtendsScene, Is.EqualTo("res://base.hscene"));
        Assert.That(doc.Implements, Contains.Item("Game.IClickable"));
        Assert.That(doc.GenericBindings["T"], Is.EqualTo("Game.Foo"));
        Assert.That(doc.ForceNonInstantiable, Is.True);
        Assert.That(doc.Root, Is.EqualTo(root));
    }

    [Test]
    public void SceneDocument_StructuralEquality_WorksCorrectly()
    {
        // Les records C# fournissent l'égalité structurelle — deux records avec
        // les mêmes valeurs doivent être considérés égaux.
        var doc1 = SimpleDocument();
        var doc2 = SimpleDocument();

        Assert.That(doc1, Is.EqualTo(doc2));
    }

    // -------------------------------------------------------------------------
    // SceneNode
    // -------------------------------------------------------------------------

    [Test]
    public void SceneNode_StoresChildren_InOrder()
    {
        var child1 = SimpleNode("child1");
        var child2 = SimpleNode("child2");
        var parent = new SceneNode(
            Id: "parent",
            TypeName: "Game.ParentNode",
            GenericBindings: new Dictionary<string, string>(),
            Properties: new Dictionary<string, object?>(),
            Children: new[] { child1, child2 }
        );

        Assert.That(parent.Children, Is.EqualTo(new SceneElement[] { child1, child2 }));
    }

    [Test]
    public void SceneNode_StoresProperties()
    {
        var node = new SceneNode(
            Id: "n",
            TypeName: "Game.MyNode",
            GenericBindings: new Dictionary<string, string>(),
            Properties: new Dictionary<string, object?> { ["speed"] = 4.5, ["name"] = "hero" },
            Children: Array.Empty<SceneElement>()
        );

        Assert.That(node.Properties["speed"], Is.EqualTo(4.5));
        Assert.That(node.Properties["name"], Is.EqualTo("hero"));
    }

    // -------------------------------------------------------------------------
    // SceneVirtualNode
    // -------------------------------------------------------------------------

    [Test]
    public void SceneVirtualNode_Required_IsStored()
    {
        var vn = new SceneVirtualNode(
            Id: "controller",
            TypeConstraint: "Game.CharacterController",
            Required: true,
            Default: null
        );

        Assert.That(vn.Required, Is.True);
        Assert.That(vn.Default, Is.Null);
    }

    [Test]
    public void SceneVirtualNode_WithDefault_StoresDefault()
    {
        var defaultNode = SimpleNode("default_ctrl");
        var vn = new SceneVirtualNode(
            Id: "controller",
            TypeConstraint: "Game.CharacterController",
            Required: false,
            Default: defaultNode
        );

        Assert.That(vn.Default, Is.EqualTo(defaultNode));
    }

    // -------------------------------------------------------------------------
    // SceneSlot
    // -------------------------------------------------------------------------

    [Test]
    public void SceneSlot_StoresVisibilityAndTarget()
    {
        var slot = new SceneSlot(
            Id: "entries",
            AcceptedType: "Game.IEntry",
            TargetNodeId: "content_grid",
            Visibility: SlotVisibility.Public,
            Items: Array.Empty<SceneElement>()
        );

        Assert.That(slot.Visibility, Is.EqualTo(SlotVisibility.Public));
        Assert.That(slot.TargetNodeId, Is.EqualTo("content_grid"));
    }

    [Test]
    public void SceneSlot_StoresItems()
    {
        var item = SimpleNode("item1");
        var slot = new SceneSlot(
            Id: "entries",
            AcceptedType: "Game.IEntry",
            TargetNodeId: "grid",
            Visibility: SlotVisibility.Protected,
            Items: new[] { item }
        );

        Assert.That(slot.Items, Contains.Item(item));
    }

    // -------------------------------------------------------------------------
    // SceneEmbeddedScene
    // -------------------------------------------------------------------------

    [Test]
    public void SceneEmbeddedScene_StoresOverrides()
    {
        var embedded = new SceneEmbeddedScene(
            Id: "weapon",
            ScenePath: "res://scenes/sword.hscene",
            TypeConstraint: "Game.WeaponNode",
            GenericBindings: new Dictionary<string, string>(),
            PropertyOverrides: new Dictionary<string, object?> { ["display_name"] = "Épée longue" },
            SlotOverrides: new Dictionary<string, IReadOnlyList<SceneElement>>()
        );

        Assert.That(embedded.ScenePath, Is.EqualTo("res://scenes/sword.hscene"));
        Assert.That(embedded.PropertyOverrides["display_name"], Is.EqualTo("Épée longue"));
        Assert.That(embedded.TypeConstraint, Is.EqualTo("Game.WeaponNode"));
    }

    // -------------------------------------------------------------------------
    // SceneLoadResult
    // -------------------------------------------------------------------------

    [Test]
    public void SceneLoadResult_CanInstantiate_TrueOnlyForInstantiable()
    {
        var instantiable = new SceneLoadResult(
            SimpleDocument(), SceneInstantiabilityStatus.Instantiable, Array.Empty<SceneDiagnostic>());

        var forced = new SceneLoadResult(
            SimpleDocument(), SceneInstantiabilityStatus.NonInstantiableForced, Array.Empty<SceneDiagnostic>());

        Assert.That(instantiable.CanInstantiate, Is.True);
        Assert.That(forced.CanInstantiate, Is.False);
    }

    [Test]
    public void SceneLoadResult_HasErrors_TrueWhenErrorDiagnosticPresent()
    {
        var error = new SceneDiagnostic("SCN0002", SceneDiagnosticSeverity.Error, "Clé manquante");
        var result = new SceneLoadResult(
            null, SceneInstantiabilityStatus.Invalid, new[] { error });

        Assert.That(result.HasErrors, Is.True);
    }

    [Test]
    public void SceneLoadResult_HasErrors_FalseWithOnlyInfoDiagnostics()
    {
        var info = new SceneDiagnostic("SCN0019", SceneDiagnosticSeverity.Info, "NonInstantiable");
        var result = new SceneLoadResult(
            SimpleDocument(), SceneInstantiabilityStatus.NonInstantiableByStructure, new[] { info });

        Assert.That(result.HasErrors, Is.False);
    }
}