using HumbleEngine.Core.Scenes;
using NUnit.Framework;

namespace HumbleEngine.Core.Tests.Scenes;

[TestFixture]
internal sealed class SceneParserTests
{
    private SceneParser _parser = null!;

    [SetUp]
    public void SetUp() => _parser = new SceneParser();

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string SimpleNodeJson(string id = "root", string type = "Game.MyNode") => $$"""
        {
          "kind": "node",
          "id": "{{id}}",
          "type": "{{type}}",
          "generic_bindings": {},
          "properties": {},
          "slots": {},
          "children": []
        }
        """;

    private static string BaseScene(string root) => $$"""
        {
          "schema_version": 1,
          "scene_kind": "base",
          "implements": [],
          "force_non_instantiable": false,
          "root": {{root}}
        }
        """;

    private static string InheritedScene(
        string extendsScene,
        string replaceVirtuals = "{}",
        string fillSlots = "{}",
        string setProperties = "{}") => $$"""
        {
          "schema_version": 1,
          "scene_kind": "inherited",
          "extends_scene": "{{extendsScene}}",
          "implements": [],
          "force_non_instantiable": false,
          "replace_virtuals": {{replaceVirtuals}},
          "fill_slots": {{fillSlots}},
          "set_properties": {{setProperties}}
        }
        """;

    // -------------------------------------------------------------------------
    // BaseScene — parsing nominal
    // -------------------------------------------------------------------------

    [Test]
    public void Parse_ValidBaseScene_ReturnsInstantiableResult()
    {
        var result = _parser.Parse(BaseScene(SimpleNodeJson()));

        Assert.That(result.Document, Is.Not.Null);
        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Instantiable));
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void Parse_BaseScene_HasRoot_AndEmptyOverrideDicts()
    {
        var result = _parser.Parse(BaseScene(SimpleNodeJson()));
        var doc = result.Document!;

        Assert.That(doc.Root, Is.Not.Null);
        Assert.That(doc.Kind, Is.EqualTo(SceneKind.Base));
        Assert.That(doc.ReplaceVirtuals, Is.Empty);
        Assert.That(doc.FillSlots, Is.Empty);
        Assert.That(doc.SetProperties, Is.Empty);
    }

    [Test]
    public void Parse_SceneNode_StoresTypeAndProperties()
    {
        var json = BaseScene("""
            {
              "kind": "node", "id": "player", "type": "Game.PlayerNode",
              "generic_bindings": {}, "slots": {},
              "properties": { "speed": 4.5, "name": "hero" },
              "children": []
            }
            """);

        var node = (SceneNode)_parser.Parse(json).Document!.Root!;

        Assert.That(node.TypeName, Is.EqualTo("Game.PlayerNode"));
        Assert.That(node.Properties["speed"], Is.EqualTo(4.5));
        Assert.That(node.Properties["name"], Is.EqualTo("hero"));
    }

    [Test]
    public void Parse_SceneNode_StoresSlots_SeparateFromChildren()
    {
        var json = BaseScene($$"""
            {
              "kind": "node", "id": "root", "type": "Game.ContainerNode",
              "generic_bindings": {}, "properties": {},
              "slots": {
                "entries": {
                  "accepted_type": "Game.IEntry",
                  "target_node_id": "grid",
                  "visibility": "public",
                  "items": []
                }
              },
              "children": [{{SimpleNodeJson("grid")}}]
            }
            """);

        var node = (SceneNode)_parser.Parse(json).Document!.Root!;

        Assert.That(node.Slots, Contains.Key("entries"));
        Assert.That(node.Slots["entries"].TargetNodeId, Is.EqualTo("grid"));
        Assert.That(node.Slots["entries"].Visibility, Is.EqualTo(SlotVisibility.Public));
        Assert.That(node.Children.Count, Is.EqualTo(1));
        Assert.That(node.Children[0].Id, Is.EqualTo("grid"));
    }

    // -------------------------------------------------------------------------
    // generic_bindings — les deux formes de TypeRef
    // -------------------------------------------------------------------------

    [Test]
    public void Parse_GenericBindings_SimpleStringForm_ProducesSimpleTypeRef()
    {
        // La forme chaîne "Game.PlayerStats" est le cas le plus courant —
        // elle doit produire un TypeRef.Simple sans arguments.
        var json = BaseScene("""
            {
              "kind": "node", "id": "root", "type": "Game.PlayerNode`1",
              "generic_bindings": { "TStats": "Game.PlayerStats" },
              "properties": {}, "slots": {}, "children": []
            }
            """);

        var node = (SceneNode)_parser.Parse(json).Document!.Root!;
        var binding = node.GenericBindings["TStats"];

        Assert.That(binding.TypeName, Is.EqualTo("Game.PlayerStats"));
        Assert.That(binding.IsGeneric, Is.False);
    }

    [Test]
    public void Parse_GenericBindings_ObjectForm_ProducesGenericTypeRef()
    {
        // La forme objet { "type": "...", "args": [...] } pour un type générique fermé.
        var json = BaseScene("""
            {
              "kind": "node", "id": "root", "type": "Game.InventoryNode`1",
              "generic_bindings": {
                "TContainer": { "type": "Game.Inventory`1", "args": ["Game.Sword"] }
              },
              "properties": {}, "slots": {}, "children": []
            }
            """);

        var node = (SceneNode)_parser.Parse(json).Document!.Root!;
        var binding = node.GenericBindings["TContainer"];

        Assert.That(binding.TypeName, Is.EqualTo("Game.Inventory`1"));
        Assert.That(binding.IsGeneric, Is.True);
        Assert.That(binding.Args[0].TypeName, Is.EqualTo("Game.Sword"));
    }

    [Test]
    public void Parse_GenericBindings_DeepNestedGeneric_IsRecursive()
    {
        // Inventory<List<Sword>> — générique profond à deux niveaux.
        var json = BaseScene("""
            {
              "kind": "node", "id": "root", "type": "Game.InventoryNode`1",
              "generic_bindings": {
                "TContainer": {
                  "type": "Game.Inventory`1",
                  "args": [
                    { "type": "System.Collections.Generic.List`1", "args": ["Game.Sword"] }
                  ]
                }
              },
              "properties": {}, "slots": {}, "children": []
            }
            """);

        var node = (SceneNode)_parser.Parse(json).Document!.Root!;
        var binding = node.GenericBindings["TContainer"];

        Assert.That(binding.TypeName, Is.EqualTo("Game.Inventory`1"));
        Assert.That(binding.Args[0].TypeName, Is.EqualTo("System.Collections.Generic.List`1"));
        Assert.That(binding.Args[0].Args[0].TypeName, Is.EqualTo("Game.Sword"));
    }

    [Test]
    public void Parse_GenericBindings_MixedForms_InSameNode()
    {
        // Un node peut mélanger les deux formes dans le même generic_bindings.
        var json = BaseScene("""
            {
              "kind": "node", "id": "root", "type": "Game.SomeNode`2",
              "generic_bindings": {
                "TSimple": "Game.Sword",
                "TComplex": { "type": "Game.Inventory`1", "args": ["Game.Shield"] }
              },
              "properties": {}, "slots": {}, "children": []
            }
            """);

        var node = (SceneNode)_parser.Parse(json).Document!.Root!;

        Assert.That(node.GenericBindings["TSimple"].IsGeneric, Is.False);
        Assert.That(node.GenericBindings["TComplex"].IsGeneric, Is.True);
    }

    [Test]
    public void Parse_GenericBindings_EmptyArgs_ProducesNonGenericTypeRef()
    {
        // Un objet avec "args": [] est équivalent à une chaîne simple.
        var json = BaseScene("""
            {
              "kind": "node", "id": "root", "type": "Game.MyNode`1",
              "generic_bindings": {
                "T": { "type": "Game.Sword", "args": [] }
              },
              "properties": {}, "slots": {}, "children": []
            }
            """);

        var node = (SceneNode)_parser.Parse(json).Document!.Root!;

        Assert.That(node.GenericBindings["T"].IsGeneric, Is.False);
        Assert.That(node.GenericBindings["T"].TypeName, Is.EqualTo("Game.Sword"));
    }

    // -------------------------------------------------------------------------
    // InheritedScene — parsing nominal
    // -------------------------------------------------------------------------

    [Test]
    public void Parse_ValidInheritedScene_HasNullRoot()
    {
        var result = _parser.Parse(InheritedScene("res://base.hscene"));

        Assert.That(result.Document!.Root, Is.Null);
        Assert.That(result.Document.Kind, Is.EqualTo(SceneKind.Inherited));
        Assert.That(result.Document.ExtendsScene, Is.EqualTo("res://base.hscene"));
    }

    [Test]
    public void Parse_ReplaceVirtuals_IsIndexedByTargetId()
    {
        var json = InheritedScene(
            extendsScene: "res://base.hscene",
            replaceVirtuals: $$"""
            { "controller": {{SimpleNodeJson("player_ctrl", "Game.PlayerController")}} }
            """);

        var doc = _parser.Parse(json).Document!;

        Assert.That(doc.ReplaceVirtuals, Contains.Key("controller"));
        Assert.That(doc.ReplaceVirtuals["controller"].Id, Is.EqualTo("player_ctrl"));
    }

    [Test]
    public void Parse_FillSlots_IsIndexedByTargetId_WithItemsList()
    {
        var json = InheritedScene(
            extendsScene: "res://base.hscene",
            fillSlots: $$"""
            { "abilities": { "items": [{{SimpleNodeJson("dash", "Game.DashAbility")}}] } }
            """);

        var doc = _parser.Parse(json).Document!;

        Assert.That(doc.FillSlots, Contains.Key("abilities"));
        Assert.That(doc.FillSlots["abilities"][0].Id, Is.EqualTo("dash"));
    }

    [Test]
    public void Parse_SetProperties_IsIndexedByNodeId_WithPropertyDict()
    {
        var json = InheritedScene(
            extendsScene: "res://base.hscene",
            setProperties: """
            { "player": { "speed": 6.0, "display_name": "Hero" }, "camera": { "fov": 90 } }
            """);

        var doc = _parser.Parse(json).Document!;

        Assert.That(doc.SetProperties["player"]["speed"], Is.EqualTo(6.0));
        Assert.That(doc.SetProperties["player"]["display_name"], Is.EqualTo("Hero"));
        Assert.That(doc.SetProperties["camera"]["fov"], Is.EqualTo(90));
    }

    // -------------------------------------------------------------------------
    // Erreurs de parsing
    // -------------------------------------------------------------------------

    [Test]
    public void Parse_InvalidJson_ReturnsSCN0001()
    {
        var result = _parser.Parse("{ not valid json }");

        Assert.That(result.Document, Is.Null);
        Assert.That(result.Status, Is.EqualTo(SceneInstantiabilityStatus.Invalid));
        Assert.That(result.Diagnostics.Any(d => d.Code == "SCN0001"), Is.True);
    }

    [Test]
    public void Parse_InvalidSceneKind_ReturnsSCN0004()
    {
        var json = BaseScene(SimpleNodeJson()).Replace("\"base\"", "\"unknown\"");

        Assert.That(_parser.Parse(json).Diagnostics.Any(d => d.Code == "SCN0004"), Is.True);
    }

    [Test]
    public void Parse_InheritedWithoutExtendsScene_ReturnsSCN0005()
    {
        var json = """
            {
              "schema_version": 1, "scene_kind": "inherited",
              "implements": [], "force_non_instantiable": false,
              "replace_virtuals": {}, "fill_slots": {}, "set_properties": {}
            }
            """;

        Assert.That(_parser.Parse(json).Diagnostics.Any(d => d.Code == "SCN0005"), Is.True);
    }

    [Test]
    public void Parse_UnknownElementKind_ReturnsSCN0018()
    {
        var json = BaseScene("""{ "kind": "unknown", "id": "root" }""");

        Assert.That(_parser.Parse(json).Diagnostics.Any(d => d.Code == "SCN0018"), Is.True);
    }

    [Test]
    public void Parse_GenericBinding_InvalidTypeRefShape_ReturnsSCN0003()
    {
        // Un tableau n'est ni une chaîne ni un objet — doit déclencher SCN0003.
        var json = BaseScene("""
            {
              "kind": "node", "id": "root", "type": "Game.MyNode`1",
              "generic_bindings": { "T": [1, 2, 3] },
              "properties": {}, "slots": {}, "children": []
            }
            """);

        Assert.That(_parser.Parse(json).Diagnostics.Any(d => d.Code == "SCN0003"), Is.True);
    }
}