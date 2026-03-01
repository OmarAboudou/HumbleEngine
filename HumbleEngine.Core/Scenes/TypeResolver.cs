using System.Collections.Concurrent;
using System.Reflection;

namespace HumbleEngine.Core.Scenes;

// =============================================================================
// Résultat de résolution
// =============================================================================

/// <summary>
/// Résultat discriminé d'une tentative de résolution de <see cref="TypeRef"/>
/// vers un <see cref="System.Type"/> C#.
/// </summary>
public abstract record TypeResolveResult
{
    /// <summary>La résolution a réussi — <see cref="Type"/> est le type C# résolu.</summary>
    public sealed record Success(Type Type) : TypeResolveResult;

    /// <summary>
    /// Le nom de type n'a été trouvé dans aucun assembly enregistré.
    /// </summary>
    public sealed record TypeNotFound(string TypeName) : TypeResolveResult;

    /// <summary>
    /// Le type a été trouvé mais les arguments génériques sont incorrects
    /// (mauvais nombre d'arguments, ou l'un des arguments n'a pas pu être résolu).
    /// </summary>
    public sealed record GenericArityMismatch(string TypeName, int Expected, int Actual) : TypeResolveResult;

    /// <summary>
    /// Un paramètre générique ouvert (ex: "TItem") a été rencontré dans le TypeRef
    /// mais n'est pas présent dans le dictionnaire de bindings fourni.
    /// </summary>
    public sealed record UnboundTypeParameter(string ParameterName) : TypeResolveResult;

    /// <summary>
    /// Les contraintes génériques déclarées sur le type ouvert ne sont pas
    /// satisfaites par le type fourni comme argument.
    /// </summary>
    public sealed record ConstraintViolation(string TypeName, string Constraint) : TypeResolveResult;

    /// <summary>Indique si la résolution a réussi.</summary>
    public bool IsSuccess => this is Success;
}

// =============================================================================
// TypeResolver
// =============================================================================

/// <summary>
/// Résout des <see cref="TypeRef"/> en <see cref="System.Type"/> C# en cherchant
/// dans un ensemble d'assemblies enregistrés explicitement.
///
/// <para>
/// Les assemblies doivent être enregistrés avant toute résolution via
/// <see cref="RegisterAssembly"/>. L'assembly <c>mscorlib</c> / <c>System.Private.CoreLib</c>
/// est toujours inclus pour les types système.
/// </para>
///
/// <para>
/// Les résultats de résolution des types ouverts (avant application des arguments
/// génériques) sont mis en cache pour éviter de parcourir les assemblies à chaque appel.
/// </para>
/// </summary>
public sealed class TypeResolver
{
    // Cache des types ouverts : "Game.Inventory`1" → Type (ouvert)
    private readonly ConcurrentDictionary<string, Type?> _openTypeCache = new();

    // Assemblies dans lesquels chercher, dans l'ordre d'enregistrement.
    private readonly List<Assembly> _assemblies = new();
    private readonly object _assembliesLock = new();

    public TypeResolver()
    {
        // L'assembly système est toujours disponible — il contient List<T>,
        // Dictionary<K,V>, et tous les types de base.
        RegisterAssembly(typeof(object).Assembly);
    }

    /// <summary>
    /// Enregistre un assembly dans lequel le resolver cherchera des types.
    /// Les doublons sont ignorés silencieusement.
    /// </summary>
    public void RegisterAssembly(Assembly assembly)
    {
        lock (_assembliesLock)
        {
            if (!_assemblies.Contains(assembly))
                _assemblies.Add(assembly);
        }
    }

    /// <summary>
    /// Résout un <see cref="TypeRef"/> en <see cref="System.Type"/>.
    ///
    /// <para>
    /// Le paramètre <paramref name="openParameters"/> permet de substituer des
    /// paramètres génériques ouverts (ex: <c>"TItem"</c>) par leurs types réels.
    /// Utile lors de la validation des <c>type_constraint</c> de <see cref="SceneVirtualNode"/>,
    /// qui peuvent référencer un paramètre générique de la scène racine.
    /// </para>
    /// </summary>
    /// <param name="typeRef">Référence de type à résoudre.</param>
    /// <param name="openParameters">
    /// Dictionnaire de substitution pour les paramètres génériques ouverts.
    /// Clé = nom du paramètre (ex: "TItem"), valeur = type C# de substitution.
    /// Null ou vide si aucun paramètre ouvert n'est en jeu.
    /// </param>
    public TypeResolveResult Resolve(TypeRef typeRef,
        IReadOnlyDictionary<string, Type>? openParameters = null)
    {
        // Cas 1 : le nom est un paramètre générique ouvert (ex: "TItem").
        // On le substitue directement depuis le dictionnaire de bindings.
        if (openParameters is not null && openParameters.TryGetValue(typeRef.TypeName, out var substituted))
        {
            // Si le TypeRef substitué a lui-même des Args, c'est incohérent —
            // un paramètre ouvert ne peut pas avoir d'arguments génériques propres.
            if (typeRef.IsGeneric)
                return new TypeResolveResult.GenericArityMismatch(typeRef.TypeName, 0, typeRef.Args.Count);

            return new TypeResolveResult.Success(substituted);
        }

        // Cas 2 : type simple (pas d'arguments génériques dans le TypeRef).
        if (!typeRef.IsGeneric)
        {
            var openType = FindOpenType(typeRef.TypeName);
            if (openType is null)
                return new TypeResolveResult.TypeNotFound(typeRef.TypeName);

            // Un type trouvé qui est encore générique ouvert est invalide ici —
            // il aurait dû avoir des Args dans le TypeRef.
            if (openType.IsGenericTypeDefinition)
                return new TypeResolveResult.GenericArityMismatch(
                    typeRef.TypeName, openType.GetGenericArguments().Length, 0);

            return new TypeResolveResult.Success(openType);
        }

        // Cas 3 : type générique fermé — on résout récursivement chaque argument,
        // puis on construit le type fermé via MakeGenericType.
        return ResolveClosedGeneric(typeRef, openParameters);
    }

    // -------------------------------------------------------------------------
    // Résolution d'un type générique fermé
    // -------------------------------------------------------------------------

    private TypeResolveResult ResolveClosedGeneric(TypeRef typeRef,
        IReadOnlyDictionary<string, Type>? openParameters)
    {
        var openType = FindOpenType(typeRef.TypeName);
        if (openType is null)
            return new TypeResolveResult.TypeNotFound(typeRef.TypeName);

        if (!openType.IsGenericTypeDefinition)
            return new TypeResolveResult.GenericArityMismatch(typeRef.TypeName, 0, typeRef.Args.Count);

        var expectedArity = openType.GetGenericArguments().Length;
        if (expectedArity != typeRef.Args.Count)
            return new TypeResolveResult.GenericArityMismatch(typeRef.TypeName, expectedArity, typeRef.Args.Count);

        // Résolution récursive de chaque argument — on s'arrête au premier échec.
        var resolvedArgs = new Type[typeRef.Args.Count];
        for (var i = 0; i < typeRef.Args.Count; i++)
        {
            var argResult = Resolve(typeRef.Args[i], openParameters);
            if (argResult is not TypeResolveResult.Success argSuccess)
                return argResult; // Propage l'erreur de l'argument

            resolvedArgs[i] = argSuccess.Type;
        }

        // Vérification des contraintes génériques déclarées sur le type ouvert.
        // MakeGenericType lèverait une ArgumentException en cas de violation —
        // on préfère la détecter proprement et retourner un résultat discriminé.
        var typeParams = openType.GetGenericArguments();
        for (var i = 0; i < typeParams.Length; i++)
        {
            var violation = CheckConstraints(typeParams[i], resolvedArgs[i]);
            if (violation is not null)
                return new TypeResolveResult.ConstraintViolation(typeRef.TypeName, violation);
        }

        try
        {
            var closedType = openType.MakeGenericType(resolvedArgs);
            return new TypeResolveResult.Success(closedType);
        }
        catch (ArgumentException ex)
        {
            // Filet de sécurité — notre vérification de contraintes devrait avoir
            // tout attrapé, mais MakeGenericType peut rejeter des cas qu'on n'a pas prévus.
            return new TypeResolveResult.ConstraintViolation(typeRef.TypeName, ex.Message);
        }
    }

    // -------------------------------------------------------------------------
    // Recherche d'un type ouvert dans les assemblies enregistrés
    // -------------------------------------------------------------------------

    /// <summary>
    /// Cherche un type par son nom dans les assemblies enregistrés.
    /// Utilise un cache pour éviter de parcourir tous les assemblies à chaque appel.
    /// Retourne le type ouvert (non fermé pour les génériques).
    /// </summary>
    private Type? FindOpenType(string typeName)
    {
        return _openTypeCache.GetOrAdd(typeName, name =>
        {
            List<Assembly> snapshot;
            lock (_assembliesLock) { snapshot = new List<Assembly>(_assemblies); }

            foreach (var assembly in snapshot)
            {
                var type = assembly.GetType(name);
                if (type is not null) return type;
            }

            return null;
        });
    }

    // -------------------------------------------------------------------------
    // Vérification des contraintes génériques
    // -------------------------------------------------------------------------

    /// <summary>
    /// Vérifie qu'un type argument satisfait les contraintes déclarées sur un
    /// paramètre générique. Retourne null si tout est satisfait, ou une description
    /// de la contrainte violée.
    /// </summary>
    private static string? CheckConstraints(Type typeParam, Type typeArg)
    {
        var constraints = typeParam.GetGenericParameterConstraints();

        foreach (var constraint in constraints)
        {
            // Une contrainte d'interface ou de classe de base :
            // le type argument doit être assignable à la contrainte.
            if (!constraint.IsAssignableFrom(typeArg))
                return $"{typeArg.Name} ne satisfait pas la contrainte {constraint.Name}";
        }

        var attrs = typeParam.GenericParameterAttributes;

        // Contrainte "class" — le type argument doit être une classe (type référence).
        if (attrs.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint)
            && typeArg.IsValueType)
            return $"{typeArg.Name} doit être un type référence (contrainte 'class')";

        // Contrainte "struct" — le type argument doit être un type valeur non nullable.
        if (attrs.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint)
            && (!typeArg.IsValueType || Nullable.GetUnderlyingType(typeArg) is not null))
            return $"{typeArg.Name} doit être un type valeur non nullable (contrainte 'struct')";

        // Contrainte "new()" — le type argument doit avoir un constructeur public sans paramètre.
        if (attrs.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint)
            && typeArg.GetConstructor(Type.EmptyTypes) is null)
            return $"{typeArg.Name} doit avoir un constructeur public sans paramètre (contrainte 'new()')";

        return null;
    }
}