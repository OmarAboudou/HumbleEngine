using HumbleEngine.Core.Scenes;
using NUnit.Framework;

namespace HumbleEngine.Core.Tests.Scenes;

/// <summary>
/// Tests d'intégration du flux complet Parser → Validator via SceneLoader.
/// Ces tests vérifient l'orchestration, pas la logique individuelle de chaque couche
/// (couverte respectivement par SceneParserTests et SceneValidatorTests).
/// </summary>
[TestFixture]
file class SceneLoaderTests
{
    private SceneLoader _loader = null!;

    [SetUp]
    public void SetUp() => _loader = new SceneLoader();

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string ValidBaseScene() => """
        {
          "schema_version": 1,
          "scene_kind": "base",
          "implements": [],
          "force_non_instantiable": false,
          "root": {
            "kind": "node",
            "id": "root",
            "type": "Game.MyNode",
            "generic_bindings": {},
            "properties": {},
            "slots": {},
            "children": []
          }
        }
        """;

    private static string ValidInheritedScene() => """
        {
          "schema_version": 1,
          "scene_kind": "inherited",
          "extends_scene": "res://base.hscene",
          "implements": [],
          "force_non_instantiable": false,
          "replace_virtuals": {},
          "fill_slots": {},
          "set_properties": {}
        }
        """;

    // -------------------------------------------------------------------------
    // Cas nominaux
    // -------------------------------------------------------------------------

    [Test]
    public void Load_ValidBaseScene_ReturnsInstantiable()
    {
        var result = _loader.Load(ValidBaseScene());

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Instantiable));
        Assert.That(result.Document, Is.Not.Null);
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void Load_ValidInheritedScene_ReturnsInstantiable()
    {
        var result = _loader.Load(ValidInheritedScene());

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Instantiable));
        Assert.That(result.Document, Is.Not.Null);
    }

    // -------------------------------------------------------------------------
    // JSON illisible — le document est null, le validator est court-circuité
    // -------------------------------------------------------------------------

    [Test]
    public void Load_InvalidJson_ReturnsInvalidWithNullDocument()
    {
        var result = _loader.Load("{ not valid }");

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Invalid));
        Assert.That(result.Document, Is.Null);
        Assert.That(result.HasErrors, Is.True);
    }

    [Test]
    public void Load_NeverThrows_RegardlessOfInput()
    {
        // Load() ne lève jamais d'exception — c'est au consommateur de décider
        // quoi faire d'un résultat invalide selon son contexte.
        Assert.DoesNotThrow(() => _loader.Load(""));
        Assert.DoesNotThrow(() => _loader.Load("null"));
        Assert.DoesNotThrow(() => _loader.Load("{ \"broken"));
    }

    // -------------------------------------------------------------------------
    // Agrégation des diagnostics parser + validator
    // -------------------------------------------------------------------------

    [Test]
    public void Load_AggregatesParserAndValidatorDiagnostics()
    {
        // Un JSON valide structurellement mais avec un NodeVirtuel required
        // sans default produit un diagnostic de validation (SCN0020) et aucun
        // de parsing. On vérifie que les deux couches communiquent correctement.
        var json = """
            {
              "schema_version": 1,
              "scene_kind": "base",
              "implements": [],
              "force_non_instantiable": false,
              "root": {
                "kind": "virtual_node",
                "id": "root",
                "type_constraint": "Game.IRoot",
                "required": true,
                "default": null
              }
            }
            """;

        var result = _loader.Load(json);

        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0020"), Is.True);
    }

    [Test]
    public void Load_DuplicateId_ReturnsInvalidWithDocument()
    {
        // Un document structurellement invalide (id dupliqué) est quand même retourné
        // avec son document — le consommateur peut l'inspecter pour afficher les erreurs.
        var json = """
            {
              "schema_version": 1,
              "scene_kind": "base",
              "implements": [],
              "force_non_instantiable": false,
              "root": {
                "kind": "node",
                "id": "root",
                "type": "Game.MyNode",
                "generic_bindings": {},
                "properties": {},
                "slots": {},
                "children": [
                  { "kind": "node", "id": "child", "type": "Game.A", "generic_bindings": {}, "properties": {}, "slots": {}, "children": [] },
                  { "kind": "node", "id": "child", "type": "Game.B", "generic_bindings": {}, "properties": {}, "slots": {}, "children": [] }
                ]
              }
            }
            """;

        var result = _loader.Load(json);

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Invalid));
        Assert.That(result.Document, Is.Not.Null);
        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0015"), Is.True);
    }

    // -------------------------------------------------------------------------
    // Statuts NonInstantiable — valides, le consommateur choisit quoi en faire
    // -------------------------------------------------------------------------

    [Test]
    public void Load_ForceNonInstantiable_ReturnsNonInstantiableForced()
    {
        var json = """
            {
              "schema_version": 1,
              "scene_kind": "base",
              "implements": [],
              "force_non_instantiable": true,
              "root": {
                "kind": "node",
                "id": "root",
                "type": "Game.MyNode",
                "generic_bindings": {},
                "properties": {},
                "slots": {},
                "children": []
              }
            }
            """;

        var result = _loader.Load(json);

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.NonInstantiableForced));
        Assert.That(result.Document, Is.Not.Null);
    }

    [Test]
    public void Load_RequiredVirtualWithoutDefault_ReturnsNonInstantiableByStructure()
    {
        var json = """
            {
              "schema_version": 1,
              "scene_kind": "base",
              "implements": [],
              "force_non_instantiable": false,
              "root": {
                "kind": "virtual_node",
                "id": "root",
                "type_constraint": "Game.IRoot",
                "required": true,
                "default": null
              }
            }
            """;

        var result = _loader.Load(json);

        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.NonInstantiableByStructure));
        Assert.That(result.Document, Is.Not.Null);
    }
}