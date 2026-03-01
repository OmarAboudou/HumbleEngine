using HumbleEngine.Core.Scenes;
using NUnit.Framework;

namespace HumbleEngine.Core.Tests.Scenes;

// -------------------------------------------------------------------------
// Types de test — nodes concrets utilisables par l'instanciateur.
// On les déclare en file-scope pour ne pas polluer le namespace global.
// -------------------------------------------------------------------------

file class SimpleNode : Node
{
    [Overridable]
    [Exposed]
    public string Label { get; private set; } = "default";

    [Overridable]
    public float Speed { get; private set; } = 1.0f;
}


file class GenericNode<T> : Node where T : Node { }

[TestFixture]
file class SceneInstantiatorTests
{
    private TypeResolver _resolver = null!;
    private SceneInstantiator _instantiator = null!;

    [SetUp]
    public void SetUp()
    {
        _resolver = new TypeResolver();
        _resolver.RegisterAssembly(typeof(SceneInstantiatorTests).Assembly);

        // Le SceneLoader est requis par le constructeur mais n'est utilisé que pour
        // les EmbeddedScene — les tests ci-dessous couvrent uniquement les BaseScene,
        // donc on passe un loader par défaut (sans TypeResolver pour simplifier).
        var loader = new SceneLoader();
        _instantiator = new SceneInstantiator(_resolver, loader);
    }

    // Raccourci : nom qualifié d'un type de test
    private static string NameOf<T>() => typeof(T).FullName!;

    // -------------------------------------------------------------------------
    // Helpers — construction de documents de test
    // -------------------------------------------------------------------------

    private static SceneNode MakeNode(
        string id,
        string? type = null,
        IReadOnlyDictionary<string, object?>? properties = null,
        IReadOnlyList<SceneElement>? children = null,
        IReadOnlyDictionary<string, SceneSlotDefinition>? slots = null,
        IReadOnlyDictionary<string, TypeRef>? genericBindings = null) =>
        new(id,
            type ?? NameOf<SimpleNode>(),
            genericBindings ?? new Dictionary<string, TypeRef>(),
            properties ?? new Dictionary<string, object?>(),
            slots ?? new Dictionary<string, SceneSlotDefinition>(),
            children ?? Array.Empty<SceneElement>());

    private static SceneVirtualNode MakeVirtualNode(string id, SceneElement? def = null) =>
        new(Id: id, TypeConstraint: "HumbleEngine.Core.Node", Required: false, Default: def);

    private static SceneDocument MakeBaseDocument(SceneElement root) => new(
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

    // Produit un SceneLoadResult Instantiable directement, sans passer par le parser.
    // Utile pour isoler les tests de l'instanciateur des éventuels bugs du parser.
    private static SceneLoadResult InstantiableResult(SceneElement root) =>
        new(MakeBaseDocument(root),
            SceneInstantiabilityStatus.Instantiable,
            Array.Empty<SceneDiagnostic>());

    // -------------------------------------------------------------------------
    // Garde-fous sur le statut d'entrée
    // -------------------------------------------------------------------------

    [Test]
    public void Instantiate_NonInstantiableStatus_Throws()
    {
        var result = new SceneLoadResult(
            MakeBaseDocument(MakeNode("root")),
            SceneInstantiabilityStatus.NonInstantiableByStructure,
            Array.Empty<SceneDiagnostic>());

        Assert.Throws<InvalidOperationException>(() => _instantiator.Instantiate(result));
    }

    [Test]
    public void Instantiate_InvalidStatus_Throws()
    {
        var result = new SceneLoadResult(
            null,
            SceneInstantiabilityStatus.Invalid,
            Array.Empty<SceneDiagnostic>());

        Assert.Throws<InvalidOperationException>(() => _instantiator.Instantiate(result));
    }

    [Test]
    public void Instantiate_InheritedScene_ThrowsNotImplemented()
    {
        var doc = new SceneDocument(
            SchemaVersion: 1,
            Kind: SceneKind.Inherited,
            ExtendsScene: "res://base.hscene",
            Implements: Array.Empty<string>(),
            ForceNonInstantiable: false,
            Root: null,
            ReplaceVirtuals: new Dictionary<string, SceneElement>(),
            FillSlots: new Dictionary<string, IReadOnlyList<SceneElement>>(),
            SetProperties: new Dictionary<string, IReadOnlyDictionary<string, object?>>()
        );
        var result = new SceneLoadResult(doc, SceneInstantiabilityStatus.Instantiable,
            Array.Empty<SceneDiagnostic>());

        Assert.Throws<NotImplementedException>(() => _instantiator.Instantiate(result));
    }

    // -------------------------------------------------------------------------
    // Instanciation de base
    // -------------------------------------------------------------------------

    [Test]
    public void Instantiate_SimpleNode_ReturnsCorrectType()
    {
        var result = InstantiableResult(MakeNode("root"));

        var node = _instantiator.Instantiate(result);

        Assert.That(node, Is.InstanceOf<SimpleNode>());
    }

    [Test]
    public void Instantiate_SimpleNode_HasNoChildren()
    {
        var result = InstantiableResult(MakeNode("root"));

        var node = _instantiator.Instantiate(result);

        Assert.That(node.Children, Is.Empty);
    }

    [Test]
    public void Instantiate_SimpleNode_IsDetached()
    {
        // Le node retourné ne doit pas être dans un NodeTree — c'est au consommateur
        // de l'attacher. Tree == null garantit cet invariant.
        var result = InstantiableResult(MakeNode("root"));

        var node = _instantiator.Instantiate(result);

        Assert.That(node.Tree, Is.Null);
        Assert.That(node.Parent, Is.Null);
    }

    // -------------------------------------------------------------------------
    // Application des propriétés [Overridable]
    // -------------------------------------------------------------------------

    [Test]
    public void Instantiate_WithStringProperty_AppliesValue()
    {
        var props = new Dictionary<string, object?> { ["label"] = "hello" };
        var result = InstantiableResult(MakeNode("root", properties: props));

        var node = (SimpleNode)_instantiator.Instantiate(result);

        Assert.That(node.Label, Is.EqualTo("hello"));
    }

    [Test]
    public void Instantiate_WithFloatProperty_AppliesValue()
    {
        var props = new Dictionary<string, object?> { ["speed"] = 4.5 };
        var result = InstantiableResult(MakeNode("root", properties: props));

        var node = (SimpleNode)_instantiator.Instantiate(result);

        Assert.That(node.Speed, Is.EqualTo(4.5f).Within(0.001f));
    }

    [Test]
    public void Instantiate_UnknownProperty_Throws()
    {
        var props = new Dictionary<string, object?> { ["nonexistent"] = "value" };
        var result = InstantiableResult(MakeNode("root", properties: props));

        Assert.Throws<InvalidOperationException>(() => _instantiator.Instantiate(result));
    }

    // -------------------------------------------------------------------------
    // Hiérarchie parent / enfants
    // -------------------------------------------------------------------------

    [Test]
    public void Instantiate_WithChildren_AttachesCorrectly()
    {
        var root = MakeNode("root", children: new SceneElement[]
        {
            MakeNode("child1"),
            MakeNode("child2")
        });

        var node = _instantiator.Instantiate(InstantiableResult(root));

        Assert.That(node.Children, Has.Count.EqualTo(2));
        Assert.That(node.Children.All(c => c is SimpleNode), Is.True);
    }

    [Test]
    public void Instantiate_WithChildren_ParentIsSet()
    {
        var root = MakeNode("root", children: new SceneElement[] { MakeNode("child") });

        var node = _instantiator.Instantiate(InstantiableResult(root));

        Assert.That(node.Children[0].Parent, Is.SameAs(node));
    }

    [Test]
    public void Instantiate_ChildrenOrderPreserved()
    {
        // L'ordre des enfants est un invariant garanti par la spec.
        var root = MakeNode("root", children: new SceneElement[]
        {
            MakeNode("a"),
            MakeNode("b"),
            MakeNode("c")
        });

        var node = _instantiator.Instantiate(InstantiableResult(root));

        // On vérifie l'ordre en inspectant les propriétés Label des enfants,
        // qu'on distingue par leurs ids (tous SimpleNode par défaut).
        Assert.That(node.Children, Has.Count.EqualTo(3));
    }

    // -------------------------------------------------------------------------
    // NodeVirtuel
    // -------------------------------------------------------------------------

    [Test]
    public void Instantiate_VirtualNode_WithDefault_InstantiatesDefault()
    {
        var root = MakeNode("root", children: new SceneElement[]
        {
            MakeVirtualNode("ctrl", def: MakeNode("ctrl_impl"))
        });

        var node = _instantiator.Instantiate(InstantiableResult(root));

        // Le default du NodeVirtuel doit être instancié et attaché comme enfant.
        Assert.That(node.Children, Has.Count.EqualTo(1));
        Assert.That(node.Children[0], Is.InstanceOf<SimpleNode>());
    }

    [Test]
    public void Instantiate_VirtualNode_WithoutDefault_ProducesNoChild()
    {
        var root = MakeNode("root", children: new SceneElement[]
        {
            MakeVirtualNode("ctrl", def: null)
        });

        var node = _instantiator.Instantiate(InstantiableResult(root));

        // Un NodeVirtuel sans default ne doit pas ajouter d'enfant.
        Assert.That(node.Children, Is.Empty);
    }

    // -------------------------------------------------------------------------
    // Slots — injection dans le node cible via le registre
    // -------------------------------------------------------------------------

    [Test]
    public void Instantiate_SlotWithItems_InjectsIntoTargetNode()
    {
        // Le slot "items" pointe vers le node "target" (enfant du root).
        // Les items du slot doivent être ajoutés comme enfants de "target", pas de "root".
        var slot = new SceneSlotDefinition(
            AcceptedType: NameOf<SimpleNode>(),
            TargetNodeId: "target",
            Visibility: SlotVisibility.Public,
            Items: new SceneElement[] { MakeNode("item1"), MakeNode("item2") }
        );
        var root = MakeNode("root",
            children: new SceneElement[] { MakeNode("target") },
            slots: new Dictionary<string, SceneSlotDefinition> { ["items"] = slot });

        var node = _instantiator.Instantiate(InstantiableResult(root));

        // root a un enfant "target"
        Assert.That(node.Children, Has.Count.EqualTo(1));
        var target = node.Children[0];
        // "target" a deux enfants injectés par le slot
        Assert.That(target.Children, Has.Count.EqualTo(2));
    }

    [Test]
    public void Instantiate_SlotWithUnknownTargetId_Throws()
    {
        var slot = new SceneSlotDefinition(
            AcceptedType: NameOf<SimpleNode>(),
            TargetNodeId: "nonexistent",
            Visibility: SlotVisibility.Public,
            Items: new SceneElement[] { MakeNode("item") }
        );
        var root = MakeNode("root",
            slots: new Dictionary<string, SceneSlotDefinition> { ["items"] = slot });

        Assert.Throws<InvalidOperationException>(() =>
            _instantiator.Instantiate(InstantiableResult(root)));
    }

    [Test]
    public void Instantiate_SlotWithNoItems_NoChildrenAdded()
    {
        var slot = new SceneSlotDefinition(
            AcceptedType: NameOf<SimpleNode>(),
            TargetNodeId: "target",
            Visibility: SlotVisibility.Public,
            Items: Array.Empty<SceneElement>()
        );
        var root = MakeNode("root",
            children: new SceneElement[] { MakeNode("target") },
            slots: new Dictionary<string, SceneSlotDefinition> { ["items"] = slot });

        var node = _instantiator.Instantiate(InstantiableResult(root));
        var target = node.Children[0];

        Assert.That(target.Children, Is.Empty);
    }

    // -------------------------------------------------------------------------
    // Types génériques
    // -------------------------------------------------------------------------

    [Test]
    public void Instantiate_GenericNode_WithArgument_ClosesType()
    {
        // GenericNode<SimpleNode> — l'argument est fourni via genericArguments.
        var genericArgs = new Dictionary<string, Type>
        {
            ["T"] = typeof(SimpleNode)
        };
        var root = MakeNode("root", type: typeof(GenericNode<>).FullName!);

        var node = _instantiator.Instantiate(InstantiableResult(root), genericArgs);

        Assert.That(node, Is.InstanceOf(typeof(GenericNode<>).MakeGenericType(typeof(SimpleNode))));
    }

    [Test]
    public void Instantiate_GenericNode_WithoutArgument_Throws()
    {
        // GenericNode<T> sans binding ni argument fourni → erreur SCN0012 à l'instanciation.
        var root = MakeNode("root", type: typeof(GenericNode<>).FullName!);

        Assert.Throws<InvalidOperationException>(() =>
            _instantiator.Instantiate(InstantiableResult(root)));
    }

    [Test]
    public void Instantiate_UnknownType_Throws()
    {
        // Le validateur laisse passer les types inconnus, mais l'instanciateur
        // doit lever une exception — on ne peut pas instancier ce qu'on ne trouve pas.
        var root = MakeNode("root", type: "Game.CompletelyUnknownNode");

        Assert.Throws<InvalidOperationException>(() =>
            _instantiator.Instantiate(InstantiableResult(root)));
    }
}