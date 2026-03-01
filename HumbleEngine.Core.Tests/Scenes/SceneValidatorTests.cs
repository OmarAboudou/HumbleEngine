using HumbleEngine.Core.Scenes;
using NUnit.Framework;

namespace HumbleEngine.Core.Tests.Scenes;

[TestFixture]
file class SceneValidatorTests
{
    private SceneValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new SceneValidator();

    // -------------------------------------------------------------------------
    // Helpers — construction de documents de test
    // -------------------------------------------------------------------------

    private static SceneNode SimpleNode(string id, string type = "Game.MyNode",
        IReadOnlyList<SceneElement>? children = null,
        IReadOnlyDictionary<string, SceneSlotDefinition>? slots = null) =>
        new(id, type,
            new Dictionary<string, TypeRef>(),
            new Dictionary<string, object?>(),
            slots ?? new Dictionary<string, SceneSlotDefinition>(),
            children ?? Array.Empty<SceneElement>());

    private static SceneVirtualNode VirtualNode(string id, bool required = false, SceneElement? def = null) =>
        new(id, "Game.IController", required, def);

    private static SceneDocument BaseDocument(SceneElement root) => new(
        SchemaVersion: 1,
        Kind: SceneKind.Base,
        ExtendsScene: null,
        Implements: Array.Empty<string>(),
        ForceNonInstantiable: false,
        Root: root,
        ReplaceVirtuals: new Dictionary<string, SceneElement>(),
        FillSlots: new Dictionary<string, IReadOnlyList<SceneElement>>(),
        SetProperties: new Dictionary<string, IReadOnlyDictionary<string, object?>>()
    );

    private static SceneDocument InheritedDocument(
        IReadOnlyDictionary<string, SceneElement>? replaceVirtuals = null,
        IReadOnlyDictionary<string, IReadOnlyList<SceneElement>>? fillSlots = null) =>
        new(
            SchemaVersion: 1,
            Kind: SceneKind.Inherited,
            ExtendsScene: "res://base.hscene",
            Implements: Array.Empty<string>(),
            ForceNonInstantiable: false,
            Root: null,
            ReplaceVirtuals: replaceVirtuals ?? new Dictionary<string, SceneElement>(),
            FillSlots: fillSlots ?? new Dictionary<string, IReadOnlyList<SceneElement>>(),
            SetProperties: new Dictionary<string, IReadOnlyDictionary<string, object?>>()
        );

    private SceneLoadResult Validate(SceneDocument doc) =>
        _validator.Validate(doc, Array.Empty<SceneDiagnostic>());

    // -------------------------------------------------------------------------
    // Cas nominal — scène valide et instanciable
    // -------------------------------------------------------------------------

    [Test]
    public void Validate_ValidBaseScene_ReturnsInstantiable()
    {
        var result = Validate(BaseDocument(SimpleNode("root")));

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Instantiable));
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void Validate_ValidInheritedScene_ReturnsInstantiable()
    {
        var result = Validate(InheritedDocument());

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Instantiable));
        Assert.That(result.Diagnostics, Is.Empty);
    }

    // -------------------------------------------------------------------------
    // ForceNonInstantiable
    // -------------------------------------------------------------------------

    [Test]
    public void Validate_ForceNonInstantiable_ReturnsNonInstantiableForced()
    {
        var doc = new SceneDocument(
            SchemaVersion: 1,
            Kind: SceneKind.Base,
            ExtendsScene: null,
            Implements: Array.Empty<string>(),
            ForceNonInstantiable: true, // ← le flag
            Root: SimpleNode("root"),
            ReplaceVirtuals: new Dictionary<string, SceneElement>(),
            FillSlots: new Dictionary<string, IReadOnlyList<SceneElement>>(),
            SetProperties: new Dictionary<string, IReadOnlyDictionary<string, object?>>()
        );

        var result = Validate(doc);

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.NonInstantiableForced));
        // ForceNonInstantiable ne produit pas d'erreur — c'est un choix explicite.
        Assert.That(result.Diagnostics, Is.Empty);
    }

    // -------------------------------------------------------------------------
    // Passe 1 — Ids dupliqués (SCN0015)
    // -------------------------------------------------------------------------

    [Test]
    public void Validate_DuplicateId_InChildren_ReturnsSCN0015()
    {
        // Deux enfants avec le même id — le second doit déclencher SCN0015.
        var root = SimpleNode("root", children: new SceneElement[]
        {
            SimpleNode("child"),
            SimpleNode("child") // ← doublon
        });

        var result = Validate(BaseDocument(root));

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Invalid));
        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0015"), Is.True);
    }

    [Test]
    public void Validate_DuplicateId_BetweenRootAndChild_ReturnsSCN0015()
    {
        // L'enfant a le même id que la racine.
        var root = SimpleNode("shared_id", children: new SceneElement[]
        {
            SimpleNode("shared_id")
        });

        var result = Validate(BaseDocument(root));

        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0015"), Is.True);
    }

    [Test]
    public void Validate_DuplicateId_InSlotItems_ReturnsSCN0015()
    {
        // Les items d'un slot partagent l'espace d'ids avec le reste de l'arbre.
        var slot = new SceneSlotDefinition(
            AcceptedType: "Game.IEntry",
            TargetNodeId: "grid",
            Visibility: SlotVisibility.Public,
            Items: new SceneElement[] { SimpleNode("duplicate"), SimpleNode("duplicate") }
        );
        var root = SimpleNode("root",
            children: new SceneElement[] { SimpleNode("grid") },
            slots: new Dictionary<string, SceneSlotDefinition> { ["entries"] = slot });

        var result = Validate(BaseDocument(root));

        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0015"), Is.True);
    }

    [Test]
    public void Validate_UniqueIds_ProducesNoDuplicateError()
    {
        var root = SimpleNode("root", children: new SceneElement[]
        {
            SimpleNode("child1"),
            SimpleNode("child2"),
            VirtualNode("ctrl")
        });

        var result = Validate(BaseDocument(root));

        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0015"), Is.False);
    }

    [Test]
    public void Validate_DuplicateId_InReplaceVirtuals_ReturnsSCN0015()
    {
        // Dans une InheritedScene, les éléments des overrides ont aussi des ids uniques.
        var doc = InheritedDocument(
            replaceVirtuals: new Dictionary<string, SceneElement>
            {
                ["ctrl1"] = SimpleNode("same_id"),
                ["ctrl2"] = SimpleNode("same_id") // ← doublon
            }
        );

        var result = Validate(doc);

        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0015"), Is.True);
    }

    // -------------------------------------------------------------------------
    // Passe 2 — NodeVirtuel required sans default (SCN0020)
    // -------------------------------------------------------------------------

    [Test]
    public void Validate_RequiredVirtual_WithoutDefault_ReturnsSCN0020()
    {
        // Un NodeVirtuel required sans default rend la scène NonInstantiableByStructure.
        var root = SimpleNode("root", children: new SceneElement[]
        {
            VirtualNode("controller", required: true, def: null)
        });

        var result = Validate(BaseDocument(root));

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.NonInstantiableByStructure));
        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0020"), Is.True);
    }

    [Test]
    public void Validate_RequiredVirtual_WithDefault_IsInstantiable()
    {
        // Avec un default, le NodeVirtuel required est satisfait — la scène est instanciable.
        var root = SimpleNode("root", children: new SceneElement[]
        {
            VirtualNode("controller", required: true, def: SimpleNode("ai_ctrl", "Game.AiController"))
        });

        var result = Validate(BaseDocument(root));

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Instantiable));
        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0020"), Is.False);
    }

    [Test]
    public void Validate_OptionalVirtual_WithoutDefault_IsInstantiable()
    {
        // Un NodeVirtuel non-required sans default est valide — l'emplacement reste vide.
        var root = SimpleNode("root", children: new SceneElement[]
        {
            VirtualNode("controller", required: false, def: null)
        });

        var result = Validate(BaseDocument(root));

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Instantiable));
    }

    [Test]
    public void Validate_RequiredVirtual_Nested_ReturnsSCN0020()
    {
        // Le validateur descend en profondeur — un required nested doit aussi être détecté.
        var child = SimpleNode("child", children: new SceneElement[]
        {
            VirtualNode("nested_ctrl", required: true)
        });
        var root = SimpleNode("root", children: new SceneElement[] { child });

        var result = Validate(BaseDocument(root));

        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0020"), Is.True);
    }

    // -------------------------------------------------------------------------
    // SCN0019 — diagnostic informatif NonInstantiableByStructure
    // -------------------------------------------------------------------------

    [Test]
    public void Validate_NonInstantiableByStructure_AddsSCN0019Info()
    {
        var root = SimpleNode("root", children: new SceneElement[]
        {
            VirtualNode("ctrl", required: true)
        });

        var result = Validate(BaseDocument(root));

        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0019"
            && d.Severity == SceneDiagnosticSeverity.Info), Is.True);
    }

    // -------------------------------------------------------------------------
    // Diagnostics du parser sont préservés
    // -------------------------------------------------------------------------

    [Test]
    public void Validate_PreservesParserDiagnostics()
    {
        // Le validateur reçoit les diagnostics du parser et les conserve dans le résultat.
        var parserError = new SceneDiagnostic("SCN0002", SceneDiagnosticSeverity.Error, "Clé manquante");
        var doc = BaseDocument(SimpleNode("root"));

        var result = _validator.Validate(doc, new[] { parserError });

        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0002"), Is.True);
        // Avec une erreur de parser, on court-circuite directement vers Invalid.
        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Invalid));
    }
}