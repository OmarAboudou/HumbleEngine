using System.Reflection;

namespace HumbleEngine.Core.Scenes;

/// <summary>
/// Instancie un <see cref="SceneDocument"/> validé en un sous-arbre de <see cref="Node"/>
/// vivants, prêts à être attachés à un <see cref="NodeTree"/>.
///
/// <para>
/// Le node racine retourné est <b>détaché</b> — il n'appartient à aucun
/// <see cref="NodeTree"/>. C'est au consommateur de l'attacher via
/// <c>nodeTree.AddChild(node)</c>, ce qui déclenchera les callbacks de cycle de vie.
/// </para>
///
/// <para>
/// L'instanciation des <see cref="SceneKind.Inherited">InheritedScene</see> (application
/// des overrides en remontant la chaîne d'héritage) n'est pas encore implémentée.
/// Seules les <see cref="SceneKind.Base">BaseScene</see> sont supportées pour l'instant.
/// </para>
/// </summary>
public sealed class SceneInstantiator
{
    private readonly TypeResolver _typeResolver;
    private readonly SceneLoader _sceneLoader;

    public SceneInstantiator(TypeResolver typeResolver, SceneLoader sceneLoader)
    {
        _typeResolver = typeResolver;
        _sceneLoader = sceneLoader;
    }

    /// <summary>
    /// Instancie une scène depuis son résultat de chargement.
    /// </summary>
    /// <param name="loadResult">
    /// Résultat de chargement produit par <see cref="SceneLoader.Load"/>.
    /// Doit avoir le statut <see cref="SceneInstantiabilityStatus.Instantiable"/>.
    /// </param>
    /// <param name="genericArguments">
    /// Arguments pour les paramètres génériques ouverts de la scène racine.
    /// Clé = nom du paramètre (ex: <c>"TItem"</c>), valeur = type C# de substitution.
    /// Null si la scène n'a pas de paramètres génériques ouverts.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Levée si le statut n'est pas <see cref="SceneInstantiabilityStatus.Instantiable"/>,
    /// si un type ne peut pas être résolu, ou si les paramètres génériques requis
    /// ne sont pas tous fournis (SCN0012).
    /// </exception>
    public Node Instantiate(
        SceneLoadResult loadResult,
        IReadOnlyDictionary<string, Type>? genericArguments = null)
    {
        if (loadResult.Status != SceneInstantiabilityStatus.Instantiable)
            throw new InvalidOperationException(
                $"Impossible d'instancier une scène avec le statut '{loadResult.Status}'. " +
                "Seul le statut 'Instantiable' est accepté.");

        if (loadResult.Document is null)
            throw new InvalidOperationException(
                "Le document de scène est null — le résultat de chargement est invalide.");

        if (loadResult.Document.Kind == SceneKind.Inherited)
            throw new NotImplementedException(
                "L'instanciation des InheritedScene n'est pas encore implémentée. " +
                "La résolution de la chaîne d'héritage fera l'objet d'une étape ultérieure.");

        if (loadResult.Document.Root is null)
            throw new InvalidOperationException("La BaseScene n'a pas de node racine.");

        var ctx = new InstantiationContext(genericArguments ?? new Dictionary<string, Type>());
        return InstantiateElement(loadResult.Document.Root, ctx);
    }

    // =========================================================================
    // Traversée récursive des éléments
    // =========================================================================

    /// <summary>
    /// Instancie un élément de scène et retourne le <see cref="Node"/> correspondant.
    /// Retourne <c>null</c> pour les <see cref="SceneVirtualNode"/> sans valeur par défaut.
    /// </summary>
    private Node? InstantiateElement(SceneElement element, InstantiationContext ctx) =>
        element switch
        {
            SceneNode node           => InstantiateNode(node, ctx),
            SceneVirtualNode vn      => InstantiateVirtualNode(vn, ctx),
            SceneEmbeddedScene scene => InstantiateEmbeddedScene(scene, ctx),
            _ => throw new InvalidOperationException(
                $"Type d'élément inconnu : {element.GetType().Name}")
        };

    // -------------------------------------------------------------------------
    // SceneNode
    // -------------------------------------------------------------------------

    private Node InstantiateNode(SceneNode sceneNode, InstantiationContext ctx)
    {
        // 1. Résoudre le type C# en tenant compte des generic_bindings et des
        //    genericArguments fournis à l'instanciation.
        var resolvedType = ResolveNodeType(sceneNode, ctx);

        // 2. Créer l'instance via Activator. On suppose que les nodes ont un
        //    constructeur sans paramètre — c'est la convention Humble (l'injection
        //    de Tree se fait via NodeTree, pas via le constructeur).
        var node = CreateNodeInstance(resolvedType, sceneNode.Id);

        // 3. Enregistrer le node dans le contexte avant de descendre dans les enfants.
        //    L'ordre est important : les slots référencent des nodes cibles qui peuvent
        //    être des enfants directs — ils doivent être dans le registre au moment
        //    où on résout les slots, ce qui se produit après la récursion sur les enfants.
        ctx.Register(sceneNode.Id, node);

        // 4. Appliquer les propriétés [Overridable] par réflexion.
        ApplyProperties(node, sceneNode.Properties, sceneNode.Id);

        // 5. Instancier les enfants et les attacher.
        //    Les enfants sont enregistrés dans le contexte au fil de leur propre
        //    instanciation (étape 3 récursive), donc ils seront trouvables lors
        //    de la résolution des slots à l'étape suivante.
        foreach (var child in sceneNode.Children)
        {
            var childNode = InstantiateElement(child, ctx);
            if (childNode is not null)
                node.AddChild(childNode);
        }

        // 6. Instancier les items des slots et les injecter dans le node cible.
        //    À ce stade, tous les enfants directs du node sont dans le registre.
        foreach (var (_, slot) in sceneNode.Slots)
            InstantiateSlotItems(slot, ctx, sceneNode.Id);

        return node;
    }

    /// <summary>
    /// Résout le type C# d'un <see cref="SceneNode"/> en appliquant ses
    /// <c>generic_bindings</c> et les <c>genericArguments</c> de l'instanciation.
    ///
    /// <para>
    /// Le mapping se fait par <b>nom</b> de paramètre générique, pas par position.
    /// On appelle <c>type.GetGenericArguments()</c> pour obtenir les paramètres dans
    /// l'ordre attendu par <c>MakeGenericType</c>, puis on résout chacun depuis les
    /// bindings ou les arguments d'instanciation.
    /// </para>
    /// </summary>
    private Type ResolveNodeType(SceneNode sceneNode, InstantiationContext ctx)
    {
        // Résolution du type ouvert (ex: "Game.InventoryNode`1" → Type ouvert).
        var openTypeResult = _typeResolver.Resolve(TypeRef.Simple(sceneNode.TypeName));

        if (openTypeResult is TypeResolveResult.TypeNotFound)
            throw new InvalidOperationException(
                $"[{sceneNode.Id}] Type introuvable : '{sceneNode.TypeName}'. " +
                "Vérifiez que l'assembly contenant ce type est enregistré dans le TypeResolver.");

        if (openTypeResult is not TypeResolveResult.Success openTypeSuccess)
            throw new InvalidOperationException(
                $"[{sceneNode.Id}] Impossible de résoudre '{sceneNode.TypeName}' : {openTypeResult}");

        var openType = openTypeSuccess.Type;

        // Si le type n'est pas générique, on le retourne directement.
        if (!openType.IsGenericTypeDefinition)
            return openType;

        // Le type est générique ouvert — on doit le fermer.
        // On récupère les paramètres dans l'ordre de déclaration C# pour MakeGenericType.
        var typeParams = openType.GetGenericArguments();
        var resolvedArgs = new Type[typeParams.Length];

        for (var i = 0; i < typeParams.Length; i++)
        {
            var paramName = typeParams[i].Name; // ex: "TItem", "TStats"

            // Priorité 1 : le binding déclaré dans le fichier de scène.
            if (sceneNode.GenericBindings.TryGetValue(paramName, out var bindingTypeRef))
            {
                // Le TypeRef peut lui-même référencer un paramètre ouvert de la scène —
                // on passe le dictionnaire d'arguments comme contexte de substitution.
                var bindingResult = _typeResolver.Resolve(bindingTypeRef, ctx.OpenParameters);

                if (bindingResult is not TypeResolveResult.Success bindingSuccess)
                    throw new InvalidOperationException(
                        $"[{sceneNode.Id}] Impossible de résoudre le binding '{paramName}' : {bindingResult}");

                resolvedArgs[i] = bindingSuccess.Type;
                continue;
            }

            // Priorité 2 : les arguments fournis à l'instanciation (paramètres libres de la scène).
            if (ctx.OpenParameters.TryGetValue(paramName, out var instArg))
            {
                resolvedArgs[i] = instArg;
                continue;
            }

            // Aucune source pour ce paramètre — SCN0012 au moment de l'instanciation.
            throw new InvalidOperationException(
                $"[{sceneNode.Id}] Le paramètre générique '{paramName}' de '{sceneNode.TypeName}' " +
                "n'est fourni ni dans les generic_bindings ni dans les genericArguments d'instanciation. " +
                "Fournissez ce paramètre via SceneInstantiator.Instantiate(genericArguments: ...).");
        }

        return openType.MakeGenericType(resolvedArgs);
    }

    /// <summary>
    /// Crée une instance du type résolu via <see cref="Activator.CreateInstance"/>.
    /// Suppose l'existence d'un constructeur public sans paramètre.
    /// </summary>
    private static Node CreateNodeInstance(Type resolvedType, string elementId)
    {
        if (!typeof(Node).IsAssignableFrom(resolvedType))
            throw new InvalidOperationException(
                $"[{elementId}] Le type '{resolvedType.FullName}' n'hérite pas de Node.");

        try
        {
            return (Node)Activator.CreateInstance(resolvedType)!;
        }
        catch (MissingMethodException)
        {
            throw new InvalidOperationException(
                $"[{elementId}] Le type '{resolvedType.FullName}' n'a pas de constructeur " +
                "public sans paramètre. Les nodes Humble doivent en avoir un.");
        }
    }

    /// <summary>
    /// Applique les valeurs des propriétés <c>[Overridable]</c> sur un node instancié.
    /// La valeur JSON brute est convertie vers le type de la propriété avant affectation.
    /// </summary>
    private static void ApplyProperties(
        Node node, IReadOnlyDictionary<string, object?> properties, string elementId)
    {
        if (properties.Count == 0) return;

        var nodeType = node.GetType();

        foreach (var (propName, rawValue) in properties)
        {
            // On cherche d'abord une propriété, puis un champ — les deux sont
            // supportés par [Overridable] selon la spec.
            var member = FindOverridableMember(nodeType, propName);

            if (member is null)
                throw new InvalidOperationException(
                    $"[{elementId}] La propriété '{propName}' n'existe pas sur '{nodeType.Name}' " +
                    "ou n'est pas marquée [Overridable].");

            SetMemberValue(node, member, rawValue, elementId, propName);
        }
    }

    /// <summary>
    /// Cherche un membre (propriété ou champ) marqué <c>[Overridable]</c> par son nom
    /// en snake_case — le nom JSON est converti en PascalCase pour la recherche C#.
    /// </summary>
    private static MemberInfo? FindOverridableMember(Type nodeType, string snakeCaseName)
    {
        // Conversion snake_case → PascalCase pour trouver le membre C#.
        // "display_name" → "DisplayName", "speed" → "Speed"
        var pascalName = string.Concat(
            snakeCaseName.Split('_')
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        var prop = nodeType.GetProperty(pascalName, flags);
        if (prop is not null && prop.GetCustomAttribute<OverridableAttribute>() is not null)
            return prop;

        var field = nodeType.GetField(pascalName, flags);
        if (field is not null && field.GetCustomAttribute<OverridableAttribute>() is not null)
            return field;

        return null;
    }

    /// <summary>
    /// Affecte une valeur à un membre (propriété ou champ) en convertissant
    /// la valeur JSON brute vers le type cible.
    /// </summary>
    private static void SetMemberValue(
        Node node, MemberInfo member, object? rawValue, string elementId, string propName)
    {
        Type targetType = member switch
        {
            PropertyInfo p => p.PropertyType,
            FieldInfo f    => f.FieldType,
            _              => throw new InvalidOperationException("Type de membre inattendu.")
        };

        var converted = ConvertValue(rawValue, targetType, elementId, propName);

        switch (member)
        {
            case PropertyInfo prop:
                // On utilise le setter même s'il est private — c'est la sémantique
                // de [Overridable] : le moteur écrit via réflexion quelle que soit
                // la visibilité du setter.
                prop.SetValue(node, converted);
                break;
            case FieldInfo field:
                field.SetValue(node, converted);
                break;
        }
    }

    /// <summary>
    /// Convertit une valeur JSON brute (bool, int, double, string) vers le type C# cible.
    /// </summary>
    private static object? ConvertValue(
        object? rawValue, Type targetType, string elementId, string propName)
    {
        if (rawValue is null) return null;

        try
        {
            // System.Convert gère les conversions numériques courantes (int→float, etc.).
            return Convert.ChangeType(rawValue, targetType);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"[{elementId}] Impossible de convertir la valeur de '{propName}' " +
                $"({rawValue.GetType().Name} → {targetType.Name}) : {ex.Message}");
        }
    }

    // -------------------------------------------------------------------------
    // Slots
    // -------------------------------------------------------------------------

    /// <summary>
    /// Instancie les items d'un slot et les injecte dans le node cible du slot.
    /// Injecter dans un slot = ajouter comme enfant du node cible (spec §7).
    /// Le node cible est retrouvé dans le registre du contexte par son id de scène.
    /// </summary>
    private void InstantiateSlotItems(
        SceneSlotDefinition slot, InstantiationContext ctx, string parentSceneId)
    {
        if (slot.Items.Count == 0) return;

        // Le TargetNodeId désigne un node déjà instancié et enregistré dans le contexte.
        // Il est garanti d'être présent car les enfants sont enregistrés avant que leurs
        // slots parents ne soient résolus (voir l'ordre dans InstantiateNode).
        var targetNode = ctx.FindById(slot.TargetNodeId);

        if (targetNode is null)
            throw new InvalidOperationException(
                $"[{parentSceneId}] Le node cible du slot '{slot.TargetNodeId}' est introuvable " +
                "dans le registre d'instanciation. Vérifiez que le TargetNodeId correspond bien " +
                "à l'id d'un node déclaré dans la scène.");

        foreach (var item in slot.Items)
        {
            var itemNode = InstantiateElement(item, ctx);
            if (itemNode is not null)
                targetNode.AddChild(itemNode);
        }
    }

    // -------------------------------------------------------------------------
    // SceneVirtualNode
    // -------------------------------------------------------------------------

    private Node? InstantiateVirtualNode(SceneVirtualNode vn, InstantiationContext ctx)
    {
        // Un NodeVirtuel sans default laisse l'emplacement vide — c'est valide
        // pour les nodes non-required. Les required sans default ont déjà été
        // rejetés par le validateur (SCN0020), on ne peut pas arriver ici avec un
        // required sans default dans une scène Instantiable.
        if (vn.Default is null) return null;

        return InstantiateElement(vn.Default, ctx);
    }

    // -------------------------------------------------------------------------
    // SceneEmbeddedScene
    // -------------------------------------------------------------------------

    private Node InstantiateEmbeddedScene(SceneEmbeddedScene embeddedScene, InstantiationContext ctx)
    {
        // 1. Charger la scène référencée.
        var loadResult = LoadEmbeddedScene(embeddedScene.ScenePath);

        // 2. Résoudre les generic_bindings de l'EmbeddedScene pour construire
        //    les genericArguments à passer à l'instanciation récursive.
        var embeddedArgs = ResolveEmbeddedGenericArgs(embeddedScene, ctx);

        // 3. Instancier récursivement la scène référencée.
        var embeddedNode = Instantiate(loadResult, embeddedArgs);

        // 4. Appliquer les overrides de propriétés déclarés sur l'EmbeddedScene.
        ApplyProperties(embeddedNode, embeddedScene.PropertyOverrides, embeddedScene.Id);

        // 5. Injecter les items dans les slots publics overridés.
        //    Note : on ne peut remplir que les slots publics depuis une scène englobante (spec §5.3).
        foreach (var (slotId, items) in embeddedScene.SlotOverrides)
            InjectSlotOverride(embeddedNode, slotId, items, ctx);

        return embeddedNode;
    }

    private SceneLoadResult LoadEmbeddedScene(string scenePath)
    {
        // TODO: le SceneLoader actuel prend du JSON en entrée, pas un chemin de fichier.
        // La résolution des chemins "res://" vers du JSON nécessite un système de ressources
        // qui sera implémenté lors de l'intégration du pipeline de ressources.
        // Pour l'instant on lève une exception explicite.
        throw new NotImplementedException(
            $"La résolution du chemin de scène '{scenePath}' n'est pas encore implémentée. " +
            "Le pipeline de ressources (res://) fera l'objet d'une étape ultérieure.");
    }

    private IReadOnlyDictionary<string, Type> ResolveEmbeddedGenericArgs(
        SceneEmbeddedScene embeddedScene, InstantiationContext ctx)
    {
        if (embeddedScene.GenericBindings.Count == 0)
            return new Dictionary<string, Type>();

        var result = new Dictionary<string, Type>();

        foreach (var (paramName, typeRef) in embeddedScene.GenericBindings)
        {
            var resolved = _typeResolver.Resolve(typeRef, ctx.OpenParameters);

            if (resolved is not TypeResolveResult.Success success)
                throw new InvalidOperationException(
                    $"[{embeddedScene.Id}] Impossible de résoudre le binding générique " +
                    $"'{paramName}' : {resolved}");

            result[paramName] = success.Type;
        }

        return result;
    }

    private void InjectSlotOverride(
        Node embeddedNode, string slotId, IReadOnlyList<SceneElement> items, InstantiationContext ctx)
    {
        // TODO: pour injecter dans un slot par son id, on a besoin d'un registre
        // des slots exposés par le node racine de la scène embedded — ce registre
        // sera construit lors de l'intégration complète de NodeSlot<T>.
        // Pour l'instant on lève une exception explicite si des overrides de slots sont présents.
        if (items.Count > 0)
            throw new NotImplementedException(
                $"L'injection de slots par override (slot '{slotId}') n'est pas encore implémentée. " +
                "L'intégration de NodeSlot<T> avec l'instanciateur fera l'objet d'une étape ultérieure.");
    }
}

// =============================================================================
// InstantiationContext
// =============================================================================

/// <summary>
/// Contexte mutable transmis lors de la traversée récursive d'instanciation.
///
/// <para>
/// Il remplit deux rôles complémentaires. D'un côté, il porte les paramètres
/// génériques ouverts fournis par le consommateur, disponibles à tous les niveaux
/// de la récursion pour fermer les types génériques rencontrés. De l'autre, il
/// maintient un registre <c>sceneId → Node</c> alimenté au fur et à mesure que
/// les nodes sont instanciés — ce registre permet à l'instanciateur de retrouver
/// le node cible d'un slot par son id de scène, sans que <see cref="Node"/> lui-même
/// ait besoin de connaître la notion de scène. Le contexte est éphémère : il n'existe
/// que le temps de l'instanciation et disparaît avec elle.
/// </para>
/// </summary>
internal sealed class InstantiationContext
{
    /// <summary>
    /// Paramètres génériques ouverts de la scène racine, fournis par le consommateur.
    /// Clé = nom du paramètre (ex: "TItem"), valeur = type C# de substitution.
    /// Utilisé aussi comme contexte de substitution dans <see cref="TypeResolver.Resolve"/>.
    /// </summary>
    public IReadOnlyDictionary<string, Type> OpenParameters { get; }

    // Le registre n'est pas exposé directement — on passe par Register/FindById
    // pour garder le contrôle sur les invariants (pas de double enregistrement, etc.)
    private readonly Dictionary<string, Node> _nodeRegistry = new();

    public InstantiationContext(IReadOnlyDictionary<string, Type> openParameters)
    {
        OpenParameters = openParameters;
    }

    /// <summary>Enregistre un node instancié sous son id de scène.</summary>
    public void Register(string sceneId, Node node) => _nodeRegistry[sceneId] = node;

    /// <summary>
    /// Tente de retrouver un node par son id de scène.
    /// Retourne <c>null</c> si l'id est inconnu — ce qui indique que le node cible
    /// n'a pas encore été instancié ou n'existe pas dans cette scène.
    /// </summary>
    public Node? FindById(string sceneId) =>
        _nodeRegistry.TryGetValue(sceneId, out var node) ? node : null;
}