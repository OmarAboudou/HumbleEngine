using System.Text.Json;
using System.Text.Json.Nodes;

namespace HumbleEngine.Core.Scenes;

/// <summary>
/// Parse un fichier .hscene (JSON) en un <see cref="SceneDocument"/> typé,
/// en accumulant les diagnostics plutôt qu'en levant des exceptions.
///
/// Le parsing est tolérant aux erreurs partielles : si un élément est malformé,
/// un diagnostic est enregistré et le parsing continue sur le reste du document.
///
/// Les <c>generic_bindings</c> acceptent deux formes JSON :
/// une chaîne simple pour les types non génériques, et un objet structuré
/// pour les types génériques fermés (potentiellement profonds).
/// </summary>
public sealed class SceneParser
{
    public SceneLoadResult Parse(string json)
    {
        var ctx = new ParseContext();

        JsonNode? root;
        try { root = JsonNode.Parse(json); }
        catch (JsonException ex)
        {
            ctx.Error("SCN0001", $"JSON illisible : {ex.Message}");
            return new SceneLoadResult(null, SceneInstantiabilityStatus.Invalid, ctx.Diagnostics);
        }

        if (root is not JsonObject rootObj)
        {
            ctx.Error("SCN0001", "La racine du fichier JSON doit être un objet non vide.");
            return new SceneLoadResult(null, SceneInstantiabilityStatus.Invalid, ctx.Diagnostics);
        }

        var document = ParseDocument(rootObj, ctx);

        var status = ctx.HasErrors
            ? SceneInstantiabilityStatus.Invalid
            : SceneInstantiabilityStatus.Instantiable;

        return new SceneLoadResult(document, status, ctx.Diagnostics);
    }

    // -------------------------------------------------------------------------
    // Document racine
    // -------------------------------------------------------------------------

    private static SceneDocument? ParseDocument(JsonObject obj, ParseContext ctx)
    {
        var schemaVersion = ctx.RequireInt(obj, "schema_version", "$");
        var sceneKindRaw = ctx.RequireString(obj, "scene_kind", "$");
        var forceNonInstantiable = obj["force_non_instantiable"]?.GetValue<bool>() ?? false;
        var implements = ParseStringList(obj, "implements");
        var extendsScene = obj["extends_scene"]?.GetValue<string>();

        var kind = sceneKindRaw switch
        {
            "base" => SceneKind.Base,
            "inherited" => SceneKind.Inherited,
            _ => (SceneKind?)null
        };

        if (kind is null && sceneKindRaw is not null)
            ctx.Error("SCN0004",
                $"Valeur de 'scene_kind' invalide : '{sceneKindRaw}'. Valeurs acceptées : 'base', 'inherited'.", "$");

        if (kind == SceneKind.Inherited && string.IsNullOrWhiteSpace(extendsScene))
            ctx.Error("SCN0005", "Une scène héritée doit déclarer 'extends_scene'.", "$");

        SceneElement? root = null;
        var replaceVirtuals = new Dictionary<string, SceneElement>();
        var fillSlots = new Dictionary<string, IReadOnlyList<SceneElement>>();
        var setProperties = new Dictionary<string, IReadOnlyDictionary<string, object?>>();

        if (kind == SceneKind.Base || kind is null)
        {
            if (!obj.ContainsKey("root"))
                ctx.Error("SCN0002", "Clé obligatoire manquante : 'root' (BaseScene).", "$");
            else
                root = ParseElement(obj["root"], "root", ctx);
        }
        else if (kind == SceneKind.Inherited)
        {
            replaceVirtuals = ParseReplaceVirtuals(obj, ctx);
            fillSlots = ParseFillSlots(obj, ctx);
            setProperties = ParseSetProperties(obj, ctx);
        }

        if (ctx.HasErrors) return null;

        return new SceneDocument(
            SchemaVersion: schemaVersion,
            Kind: kind ?? SceneKind.Base,
            ExtendsScene: extendsScene,
            Implements: implements,
            ForceNonInstantiable: forceNonInstantiable,
            Root: root,
            ReplaceVirtuals: replaceVirtuals,
            FillSlots: fillSlots,
            SetProperties: setProperties
        );
    }

    // -------------------------------------------------------------------------
    // Dispatch polymorphe des éléments par "kind"
    // -------------------------------------------------------------------------

    private static SceneElement? ParseElement(JsonNode? node, string path, ParseContext ctx)
    {
        if (node is not JsonObject obj)
        {
            ctx.Error("SCN0003", "Un élément de scène doit être un objet JSON.", path);
            return null;
        }

        var kind = ctx.RequireString(obj, "kind", path);

        return kind switch
        {
            "node" => ParseNode(obj, path, ctx),
            "virtual_node" => ParseVirtualNode(obj, path, ctx),
            "embedded_scene" => ParseEmbeddedScene(obj, path, ctx),
            null => null,
            _ => UnknownKind(kind, path, ctx)
        };
    }

    private static SceneElement? UnknownKind(string kind, string path, ParseContext ctx)
    {
        ctx.Error("SCN0018", $"Valeur de 'kind' inconnue : '{kind}'.", path);
        return null;
    }

    // -------------------------------------------------------------------------
    // Parsers d'éléments
    // -------------------------------------------------------------------------

    private static SceneNode? ParseNode(JsonObject obj, string path, ParseContext ctx)
    {
        var id = ctx.RequireString(obj, "id", path);
        var typeName = ctx.RequireString(obj, "type", path);
        var genericBindings = ParseGenericBindings(obj, path, ctx);
        var properties = ParseProperties(obj);
        var slots = ParseSlots(obj, path, ctx);
        var children = ParseElementList(obj, "children", path, ctx);

        if (id is null || typeName is null) return null;

        return new SceneNode(id, typeName, genericBindings, properties, slots, children);
    }

    private static SceneVirtualNode? ParseVirtualNode(JsonObject obj, string path, ParseContext ctx)
    {
        var id = ctx.RequireString(obj, "id", path);
        var typeConstraint = ctx.RequireString(obj, "type_constraint", path);
        var required = obj["required"]?.GetValue<bool>() ?? false;
        var defaultElement = obj.ContainsKey("default") && obj["default"] is not null
            ? ParseElement(obj["default"], $"{path}.default", ctx)
            : null;

        if (id is null || typeConstraint is null) return null;

        return new SceneVirtualNode(id, typeConstraint, required, defaultElement);
    }

    private static SceneEmbeddedScene? ParseEmbeddedScene(JsonObject obj, string path, ParseContext ctx)
    {
        var id = ctx.RequireString(obj, "id", path);
        var scenePath = ctx.RequireString(obj, "scene_path", path);
        var genericBindings = ParseGenericBindings(obj, path, ctx);
        var propertyOverrides = new Dictionary<string, object?>();
        var slotOverrides = new Dictionary<string, IReadOnlyList<SceneElement>>();

        if (obj["overrides"] is JsonObject overridesObj)
        {
            if (overridesObj["properties"] is JsonObject propsObj)
                foreach (var (key, value) in propsObj)
                    propertyOverrides[key] = ExtractPrimitiveValue(value);

            if (overridesObj["slots"] is JsonObject slotsObj)
                foreach (var (slotId, slotNode) in slotsObj)
                {
                    var items = new List<SceneElement>();
                    if (slotNode is JsonObject slotObj && slotObj["items"] is JsonArray arr)
                        for (var i = 0; i < arr.Count; i++)
                        {
                            var el = ParseElement(arr[i], $"{path}.overrides.slots.{slotId}.items[{i}]", ctx);
                            if (el is not null) items.Add(el);
                        }
                    slotOverrides[slotId] = items;
                }
        }

        if (id is null || scenePath is null) return null;

        return new SceneEmbeddedScene(id, scenePath, genericBindings, propertyOverrides, slotOverrides);
    }

    // -------------------------------------------------------------------------
    // Parsing des generic_bindings — deux formes acceptées
    // -------------------------------------------------------------------------

    /// <summary>
    /// Parse le dictionnaire <c>generic_bindings</c> d'un node ou d'une EmbeddedScene.
    /// Chaque valeur peut être une chaîne simple (type non générique) ou un objet
    /// structuré récursif (type générique fermé).
    /// </summary>
    private static IReadOnlyDictionary<string, TypeRef> ParseGenericBindings(
        JsonObject obj, string path, ParseContext ctx)
    {
        var result = new Dictionary<string, TypeRef>();

        if (obj["generic_bindings"] is not JsonObject bindingsObj) return result;

        foreach (var (paramName, valueNode) in bindingsObj)
        {
            var bindingPath = $"{path}.generic_bindings.{paramName}";
            var typeRef = ParseTypeRef(valueNode, bindingPath, ctx);
            if (typeRef is not null)
                result[paramName] = typeRef;
        }

        return result;
    }

    /// <summary>
    /// Parse un <see cref="TypeRef"/> depuis un nœud JSON.
    /// Accepte deux formes :
    /// <list type="bullet">
    ///   <item>Une chaîne JSON : <c>"Game.Sword"</c> → <c>TypeRef.Simple("Game.Sword")</c></item>
    ///   <item>Un objet JSON : <c>{ "type": "Game.Inventory`1", "args": ["Game.Sword"] }</c></item>
    /// </list>
    /// La deuxième forme est récursive — chaque élément de <c>args</c> est lui-même
    /// parsé comme un <see cref="TypeRef"/>, permettant des génériques arbitrairement profonds.
    /// </summary>
    private static TypeRef? ParseTypeRef(JsonNode? node, string path, ParseContext ctx)
    {
        // Forme simple : "Game.Sword"
        if (node is JsonValue val && val.TryGetValue<string>(out var simpleTypeName))
        {
            if (string.IsNullOrWhiteSpace(simpleTypeName))
            {
                ctx.Error("SCN0003", "Le nom de type ne peut pas être vide.", path);
                return null;
            }
            return TypeRef.Simple(simpleTypeName);
        }

        // Forme structurée : { "type": "Game.Inventory`1", "args": [...] }
        if (node is JsonObject obj)
        {
            var typeName = ctx.RequireString(obj, "type", path);
            if (typeName is null) return null;

            var args = new List<TypeRef>();

            if (obj["args"] is JsonArray argsArray)
            {
                for (var i = 0; i < argsArray.Count; i++)
                {
                    // Récursion : chaque argument est lui-même un TypeRef.
                    var arg = ParseTypeRef(argsArray[i], $"{path}.args[{i}]", ctx);
                    if (arg is not null) args.Add(arg);
                }
            }

            return new TypeRef(typeName, args);
        }

        ctx.Error("SCN0003",
            "Un TypeRef doit être une chaîne (type simple) ou un objet { \"type\", \"args\" } (type générique).",
            path);
        return null;
    }

    // -------------------------------------------------------------------------
    // Overrides d'une InheritedScene
    // -------------------------------------------------------------------------

    private static Dictionary<string, SceneElement> ParseReplaceVirtuals(
        JsonObject obj, ParseContext ctx)
    {
        var result = new Dictionary<string, SceneElement>();

        if (obj["replace_virtuals"] is not JsonObject dict) return result;

        foreach (var (targetId, value) in dict)
        {
            var element = ParseElement(value, $"replace_virtuals.{targetId}", ctx);
            if (element is not null)
                result[targetId] = element;
        }

        return result;
    }

    private static Dictionary<string, IReadOnlyList<SceneElement>> ParseFillSlots(
        JsonObject obj, ParseContext ctx)
    {
        var result = new Dictionary<string, IReadOnlyList<SceneElement>>();

        if (obj["fill_slots"] is not JsonObject dict) return result;

        foreach (var (targetId, value) in dict)
        {
            var path = $"fill_slots.{targetId}";
            var items = new List<SceneElement>();

            if (value is JsonObject slotObj && slotObj["items"] is JsonArray arr)
                for (var i = 0; i < arr.Count; i++)
                {
                    var el = ParseElement(arr[i], $"{path}.items[{i}]", ctx);
                    if (el is not null) items.Add(el);
                }
            else
                ctx.Error("SCN0003",
                    $"L'entrée '{targetId}' de 'fill_slots' doit être un objet avec une clé 'items'.", path);

            result[targetId] = items;
        }

        return result;
    }

    private static Dictionary<string, IReadOnlyDictionary<string, object?>> ParseSetProperties(
        JsonObject obj, ParseContext ctx)
    {
        var result = new Dictionary<string, IReadOnlyDictionary<string, object?>>();

        if (obj["set_properties"] is not JsonObject dict) return result;

        foreach (var (targetId, value) in dict)
        {
            var path = $"set_properties.{targetId}";
            var props = new Dictionary<string, object?>();

            if (value is not JsonObject propsObj)
            {
                ctx.Error("SCN0003", $"L'entrée '{targetId}' de 'set_properties' doit être un objet.", path);
                continue;
            }

            foreach (var (propName, propValue) in propsObj)
                props[propName] = ExtractPrimitiveValue(propValue);

            result[targetId] = props;
        }

        return result;
    }

    // -------------------------------------------------------------------------
    // Helpers de parsing
    // -------------------------------------------------------------------------

    private static IReadOnlyDictionary<string, SceneSlotDefinition> ParseSlots(
        JsonObject obj, string path, ParseContext ctx)
    {
        var result = new Dictionary<string, SceneSlotDefinition>();

        if (obj["slots"] is not JsonObject slotsObj) return result;

        foreach (var (slotId, slotNode) in slotsObj)
        {
            var slotPath = $"{path}.slots.{slotId}";

            if (slotNode is not JsonObject slotObj)
            {
                ctx.Error("SCN0003", $"La définition du slot '{slotId}' doit être un objet JSON.", slotPath);
                continue;
            }

            var acceptedType = ctx.RequireString(slotObj, "accepted_type", slotPath);
            var targetNodeId = ctx.RequireString(slotObj, "target_node_id", slotPath);
            var visibilityRaw = slotObj["visibility"]?.GetValue<string>() ?? "public";

            var visibility = visibilityRaw switch
            {
                "public" => SlotVisibility.Public,
                "protected" => SlotVisibility.Protected,
                _ => (SlotVisibility?)null
            };

            if (visibility is null)
                ctx.Error("SCN0003",
                    $"Valeur de 'visibility' invalide : '{visibilityRaw}'. Valeurs acceptées : 'public', 'protected'.",
                    slotPath);

            var items = ParseElementList(slotObj, "items", slotPath, ctx);

            if (acceptedType is null || targetNodeId is null) continue;

            result[slotId] = new SceneSlotDefinition(
                acceptedType, targetNodeId,
                visibility ?? SlotVisibility.Public,
                items);
        }

        return result;
    }

    private static IReadOnlyList<SceneElement> ParseElementList(
        JsonObject obj, string key, string path, ParseContext ctx)
    {
        var result = new List<SceneElement>();
        if (obj[key] is not JsonArray arr) return result;

        for (var i = 0; i < arr.Count; i++)
        {
            var el = ParseElement(arr[i], $"{path}.{key}[{i}]", ctx);
            if (el is not null) result.Add(el);
        }

        return result;
    }

    private static IReadOnlyDictionary<string, object?> ParseProperties(JsonObject obj)
    {
        var result = new Dictionary<string, object?>();
        if (obj["properties"] is not JsonObject propsObj) return result;

        foreach (var (key, value) in propsObj)
            result[key] = ExtractPrimitiveValue(value);

        return result;
    }

    private static IReadOnlyList<string> ParseStringList(JsonObject obj, string key)
    {
        var result = new List<string>();
        if (obj[key] is not JsonArray arr) return result;

        foreach (var item in arr)
            if (item?.GetValue<string>() is string s)
                result.Add(s);

        return result;
    }

    /// <summary>
    /// Extrait une valeur primitive depuis un nœud JSON.
    /// L'ordre est important : bool avant int, int avant double,
    /// pour éviter que 4 soit lu comme 4.0.
    /// </summary>
    private static object? ExtractPrimitiveValue(JsonNode? node)
    {
        if (node is not JsonValue v) return node;

        // GetValue<T> retourne le primitif C# natif, pas un wrapper.
        // L'ordre bool → int → double → string est important.
        if (v.TryGetValue<bool>(out var b))   return b;
        if (v.TryGetValue<long>(out var l))   return l;
        if (v.TryGetValue<double>(out var d)) return d;
        if (v.TryGetValue<string>(out var s)) return s;

        return node;
    }
}

// =============================================================================
// ParseContext
// =============================================================================

/// <summary>
/// Contexte mutable partagé par toutes les fonctions de parsing.
/// Accumule les diagnostics et fournit des helpers pour lire les valeurs
/// JSON requises avec enregistrement automatique des erreurs.
/// </summary>
internal sealed class ParseContext
{
    private readonly List<SceneDiagnostic> _diagnostics = new();

    public IReadOnlyList<SceneDiagnostic> Diagnostics => _diagnostics;
    public bool HasErrors => _diagnostics.Any(d => d.Severity == SceneDiagnosticSeverity.Error);

    public void Error(string code, string message, string? jsonPath = null) =>
        _diagnostics.Add(new SceneDiagnostic(code, SceneDiagnosticSeverity.Error, message, jsonPath));

    public string? RequireString(JsonObject obj, string key, string path)
    {
        if (!obj.ContainsKey(key)) { Error("SCN0002", $"Clé obligatoire manquante : '{key}'.", path); return null; }
        try { return obj[key]!.GetValue<string>(); }
        catch { Error("SCN0003", $"La clé '{key}' doit être une chaîne.", path); return null; }
    }

    public int RequireInt(JsonObject obj, string key, string path)
    {
        if (!obj.ContainsKey(key)) { Error("SCN0002", $"Clé obligatoire manquante : '{key}'.", path); return 0; }
        try { return obj[key]!.GetValue<int>(); }
        catch { Error("SCN0003", $"La clé '{key}' doit être un entier.", path); return 0; }
    }
}