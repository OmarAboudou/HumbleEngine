using HumbleEngine.Core.Scenes;
using NUnit.Framework;

namespace HumbleEngine.Core.Tests.Scenes;

// Types de test déclarés localement pour simuler des types utilisateur.
// On reprend la même convention que TypeResolverTests pour la cohérence.
file interface ITestController { }
file abstract class AbstractController : ITestController { }
file class ConcreteController : ITestController { }
file class GenericNode<T> where T : ITestController { }


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
        IReadOnlyDictionary<string, SceneSlotDefinition>? slots = null,
        IReadOnlyDictionary<string, TypeRef>? genericBindings = null) =>
        new(id, type,
            genericBindings ?? new Dictionary<string, TypeRef>(),
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

    // Retourne le nom qualifié complet d'un type de test, utilisable par le TypeResolver.
    private static string NameOf<T>() => typeof(T).FullName!;

    // Crée un validateur avec passe 3 active, en enregistrant l'assembly de test.
    private static SceneValidator ValidatorWithTypeResolution()
    {
        var resolver = new TypeResolver();
        resolver.RegisterAssembly(typeof(SceneValidatorTests).Assembly);
        return new SceneValidator(resolver);
    }

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
            ForceNonInstantiable: true,
            Root: SimpleNode("root"),
            ReplaceVirtuals: new Dictionary<string, SceneElement>(),
            FillSlots: new Dictionary<string, IReadOnlyList<SceneElement>>(),
            SetProperties: new Dictionary<string, IReadOnlyDictionary<string, object?>>()
        );

        var result = Validate(doc);

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.NonInstantiableForced));
        Assert.That(result.Diagnostics, Is.Empty);
    }

    // -------------------------------------------------------------------------
    // Passe 1 — Ids dupliqués (SCN0015)
    // -------------------------------------------------------------------------

    [Test]
    public void Validate_DuplicateId_InChildren_ReturnsSCN0015()
    {
        var root = SimpleNode("root", children: new SceneElement[]
        {
            SimpleNode("child"),
            SimpleNode("child")
        });

        var result = Validate(BaseDocument(root));

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Invalid));
        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0015"), Is.True);
    }

    [Test]
    public void Validate_DuplicateId_BetweenRootAndChild_ReturnsSCN0015()
    {
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
        var doc = InheritedDocument(
            replaceVirtuals: new Dictionary<string, SceneElement>
            {
                ["ctrl1"] = SimpleNode("same_id"),
                ["ctrl2"] = SimpleNode("same_id")
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
        var parserError = new SceneDiagnostic("SCN0002", SceneDiagnosticSeverity.Error, "Clé manquante");
        var doc = BaseDocument(SimpleNode("root"));

        var result = _validator.Validate(doc, new[] { parserError });

        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0002"), Is.True);
        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Invalid));
    }

    // -------------------------------------------------------------------------
    // Passe 3 — Validation des types C# (SCN0008, SCN0011, SCN0012)
    //
    // Ces tests utilisent ValidatorWithTypeResolution() pour activer la passe 3.
    // Les types de test (ConcreteController, AbstractController, etc.) sont déclarés
    // en tête de fichier avec le modificateur "file" pour éviter la pollution du namespace.
    // -------------------------------------------------------------------------

    [Test]
    public void Validate_WithTypeResolver_ConcreteType_IsInstantiable()
    {
        // Un type concret résolvable et non abstrait — la passe 3 ne doit pas produire d'erreur.
        var validator = ValidatorWithTypeResolution();
        var root = SimpleNode("root", type: NameOf<ConcreteController>());

        var result = validator.Validate(BaseDocument(root), Array.Empty<SceneDiagnostic>());

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Instantiable));
        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0008"), Is.False);
    }

    [Test]
    public void Validate_WithTypeResolver_AbstractType_ReturnsSCN0008()
    {
        // Un type abstrait produit SCN0008 et NonInstantiableByStructure.
        // La scène reste chargeable et héritable — c'est le comportement attendu
        // pour une scène de base abstraite destinée à être spécialisée.
        var validator = ValidatorWithTypeResolution();
        var root = SimpleNode("root", type: NameOf<AbstractController>());

        var result = validator.Validate(BaseDocument(root), Array.Empty<SceneDiagnostic>());

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.NonInstantiableByStructure));
        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0008"), Is.True);
        Assert.That(result.Diagnostics.First(d => d.Code == "SCN0008").ElementId, Is.EqualTo("root"));
    }

    [Test]
    public void Validate_WithTypeResolver_Interface_ReturnsSCN0008()
    {
        // Une interface est abstraite au sens C# (IsAbstract = true).
        var validator = ValidatorWithTypeResolution();
        var root = SimpleNode("root", type: NameOf<ITestController>());

        var result = validator.Validate(BaseDocument(root), Array.Empty<SceneDiagnostic>());

        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0008"), Is.True);
    }

    [Test]
    public void Validate_WithTypeResolver_OpenGenericWithoutBindings_ProducesNoError()
    {
        // Un type générique ouvert sans bindings est valide à ce stade — ses paramètres
        // peuvent correspondre aux paramètres génériques de la scène racine et seront
        // fournis lors de l'appel à Instantiate(GenericTypeArguments).
        // SCN0012 sera vérifié à l'instanciation, pas au chargement.
        var validator = ValidatorWithTypeResolution();
        var root = SimpleNode("root", type: typeof(GenericNode<>).FullName!);

        var result = validator.Validate(BaseDocument(root), Array.Empty<SceneDiagnostic>());

        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0012"), Is.False);
    }

    [Test]
    public void Validate_WithTypeResolver_GenericBindingConstraintViolation_ReturnsSCN0011()
    {
        // GenericNode<T> where T : ITestController — on passe System.String
        // qui n'implémente pas ITestController → SCN0011.
        var validator = ValidatorWithTypeResolution();
        var bindings = new Dictionary<string, TypeRef>
        {
            ["T"] = TypeRef.Simple("System.String")
        };
        var root = SimpleNode("root", type: typeof(GenericNode<>).FullName!, genericBindings: bindings);

        var result = validator.Validate(BaseDocument(root), Array.Empty<SceneDiagnostic>());

        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0011"), Is.True);
    }

    [Test]
    public void Validate_WithTypeResolver_UnknownType_ProducesNoError()
    {
        // Un type inconnu ne produit pas d'erreur — l'assembly utilisateur peut
        // ne pas encore être enregistré. On ne bloque pas le chargement de la scène.
        var validator = ValidatorWithTypeResolution();
        var root = SimpleNode("root", type: "Game.UnknownNode");

        var result = validator.Validate(BaseDocument(root), Array.Empty<SceneDiagnostic>());

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Instantiable));
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void Validate_WithTypeResolver_AbstractType_InNestedChild_ReturnsSCN0008()
    {
        // La passe 3 descend récursivement dans les enfants.
        var child = SimpleNode("child", type: NameOf<AbstractController>());
        var root = SimpleNode("root", children: new SceneElement[] { child });
        var validator = ValidatorWithTypeResolution();

        var result = validator.Validate(BaseDocument(root), Array.Empty<SceneDiagnostic>());

        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0008"
            && d.ElementId == "child"), Is.True);
    }

    [Test]
    public void Validate_WithoutTypeResolver_AbstractType_ProducesNoError()
    {
        // Sans TypeResolver, la passe 3 est ignorée — même un type abstrait passe.
        var root = SimpleNode("root", type: NameOf<AbstractController>());

        var result = Validate(BaseDocument(root));

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Instantiable));
        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0008"), Is.False);
    }

    [Test]
    public void Validate_WithTypeResolver_ForceNonInstantiable_SkipsTypePasse()
    {
        // force_non_instantiable = true court-circuite avant la passe 3 —
        // SCN0008 n'est pas émis même avec un type abstrait.
        var validator = ValidatorWithTypeResolution();
        var doc = new SceneDocument(
            SchemaVersion: 1,
            Kind: SceneKind.Base,
            ExtendsScene: null,
            Implements: Array.Empty<string>(),
            ForceNonInstantiable: true,
            Root: SimpleNode("root", type: NameOf<AbstractController>()),
            ReplaceVirtuals: new Dictionary<string, SceneElement>(),
            FillSlots: new Dictionary<string, IReadOnlyList<SceneElement>>(),
            SetProperties: new Dictionary<string, IReadOnlyDictionary<string, object?>>()
        );

        var result = validator.Validate(doc, Array.Empty<SceneDiagnostic>());

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.NonInstantiableForced));
        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0008"), Is.False);
    }
}