using System.Reflection;
using HumbleEngine.Core.Scenes;
using NUnit.Framework;

namespace HumbleEngine.Core.Tests.Scenes;

// Types de test déclarés dans le namespace de test pour simuler des types utilisateur.
// On les utilise à la place de types comme "Game.Sword" qui n'existent pas réellement.
file interface ITestItem { }
file interface ITestContainer<T> { }
file class TestSword : ITestItem { }
file class TestShield : ITestItem { }
file class TestInventory<TItem> where TItem : ITestItem { }
file class TestBox<TItem, TLabel> where TItem : class, new() { }
file struct TestValueItem { } // struct — pour tester la contrainte 'class'
file class TestNoPublicCtor { private TestNoPublicCtor() { } } // pour tester new()

[TestFixture]
file class TypeResolverTests
{
    private TypeResolver _resolver = null!;
    private Assembly _testAssembly = null!;

    [SetUp]
    public void SetUp()
    {
        _resolver = new TypeResolver();
        _testAssembly = typeof(TypeResolverTests).Assembly;
        _resolver.RegisterAssembly(_testAssembly);
    }

    // Nom qualifié complet d'un type de test déclaré dans ce fichier.
    // Les types "file" ont un nom interne généré par le compilateur — on
    // les retrouve via réflexion plutôt qu'en codant leur nom en dur.
    private static string NameOf<T>() => typeof(T).FullName!;

    // -------------------------------------------------------------------------
    // Types simples — résolution nominale
    // -------------------------------------------------------------------------

    [Test]
    public void Resolve_SimpleType_ReturnsSuccess()
    {
        var result = _resolver.Resolve(TypeRef.Simple(NameOf<TestSword>()));

        Assert.That(result, Is.InstanceOf<TypeResolveResult.Success>());
        Assert.That(((TypeResolveResult.Success)result).Type, Is.EqualTo(typeof(TestSword)));
    }

    [Test]
    public void Resolve_SystemType_ReturnsSuccess()
    {
        // L'assembly système est toujours enregistré — string doit toujours se résoudre.
        var result = _resolver.Resolve(TypeRef.Simple("System.String"));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(((TypeResolveResult.Success)result).Type, Is.EqualTo(typeof(string)));
    }

    [Test]
    public void Resolve_UnknownType_ReturnsTypeNotFound()
    {
        var result = _resolver.Resolve(TypeRef.Simple("Game.NonExistentType"));

        Assert.That(result, Is.InstanceOf<TypeResolveResult.TypeNotFound>());
        Assert.That(((TypeResolveResult.TypeNotFound)result).TypeName, Is.EqualTo("Game.NonExistentType"));
    }

    // -------------------------------------------------------------------------
    // Types génériques fermés — résolution nominale
    // -------------------------------------------------------------------------

    [Test]
    public void Resolve_ClosedGeneric_ReturnsSuccess()
    {
        // TestInventory<TestSword>
        var typeRef = new TypeRef(NameOf<TestInventory<TestSword>>()[..^2] + "`1",
            new[] { TypeRef.Simple(NameOf<TestSword>()) });

        // On construit le TypeRef correctement : le nom du type ouvert + les args.
        var openName = typeof(TestInventory<>).FullName!;
        var correctRef = new TypeRef(openName, new[] { TypeRef.Simple(NameOf<TestSword>()) });

        var result = _resolver.Resolve(correctRef);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(((TypeResolveResult.Success)result).Type,
            Is.EqualTo(typeof(TestInventory<TestSword>)));
    }

    [Test]
    public void Resolve_ClosedGeneric_TwoArgs_ReturnsSuccess()
    {
        var openName = typeof(TestBox<,>).FullName!;
        var typeRef = new TypeRef(openName, new[]
        {
            TypeRef.Simple(NameOf<TestSword>()),   // TItem : class, new()
            TypeRef.Simple("System.String")         // TLabel (pas de contrainte)
        });

        var result = _resolver.Resolve(typeRef);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(((TypeResolveResult.Success)result).Type,
            Is.EqualTo(typeof(TestBox<TestSword, string>)));
    }

    [Test]
    public void Resolve_DeepGeneric_IsRecursive()
    {
        // List<TestSword> — System.Collections.Generic.List`1 avec TestSword comme arg
        var listOpenName = typeof(List<>).FullName!;
        var typeRef = new TypeRef(listOpenName, new[] { TypeRef.Simple(NameOf<TestSword>()) });

        var result = _resolver.Resolve(typeRef);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(((TypeResolveResult.Success)result).Type, Is.EqualTo(typeof(List<TestSword>)));
    }

    // -------------------------------------------------------------------------
    // Substitution de paramètres ouverts
    // -------------------------------------------------------------------------

    [Test]
    public void Resolve_OpenParameter_SubstitutedFromDictionary()
    {
        // "TItem" est un paramètre générique ouvert de la scène racine —
        // le resolver le substitue depuis le dictionnaire fourni.
        var openParams = new Dictionary<string, Type> { ["TItem"] = typeof(TestSword) };

        var result = _resolver.Resolve(TypeRef.Simple("TItem"), openParams);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(((TypeResolveResult.Success)result).Type, Is.EqualTo(typeof(TestSword)));
    }

    [Test]
    public void Resolve_OpenParameter_NotInDictionary_ReturnsTypeNotFound()
    {
        // Sans dictionnaire, "TItem" est traité comme un nom de type ordinaire,
        // qui ne sera pas trouvé dans les assemblies.
        var result = _resolver.Resolve(TypeRef.Simple("TItem"));

        Assert.That(result, Is.InstanceOf<TypeResolveResult.TypeNotFound>());
    }

    // -------------------------------------------------------------------------
    // Erreurs d'arité générique
    // -------------------------------------------------------------------------

    [Test]
    public void Resolve_GenericTypeWithoutArgs_ReturnsGenericArityMismatch()
    {
        // TestInventory<> est un type générique ouvert — sans args dans le TypeRef,
        // c'est une erreur d'arité (attendu 1 argument, reçu 0).
        var openName = typeof(TestInventory<>).FullName!;
        var result = _resolver.Resolve(TypeRef.Simple(openName));

        Assert.That(result, Is.InstanceOf<TypeResolveResult.GenericArityMismatch>());
        var mismatch = (TypeResolveResult.GenericArityMismatch)result;
        Assert.That(mismatch.Expected, Is.EqualTo(1));
        Assert.That(mismatch.Actual, Is.EqualTo(0));
    }

    [Test]
    public void Resolve_TooManyGenericArgs_ReturnsGenericArityMismatch()
    {
        // TestInventory<T> attend 1 argument, on en fournit 2.
        var openName = typeof(TestInventory<>).FullName!;
        var typeRef = new TypeRef(openName, new[]
        {
            TypeRef.Simple(NameOf<TestSword>()),
            TypeRef.Simple(NameOf<TestShield>())
        });

        var result = _resolver.Resolve(typeRef);

        Assert.That(result, Is.InstanceOf<TypeResolveResult.GenericArityMismatch>());
        Assert.That(((TypeResolveResult.GenericArityMismatch)result).Expected, Is.EqualTo(1));
        Assert.That(((TypeResolveResult.GenericArityMismatch)result).Actual, Is.EqualTo(2));
    }

    // -------------------------------------------------------------------------
    // Vérification des contraintes génériques
    // -------------------------------------------------------------------------

    [Test]
    public void Resolve_ConstraintViolation_InterfaceConstraint()
    {
        // TestInventory<TItem> where TItem : ITestItem
        // On passe string qui n'implémente pas ITestItem → ConstraintViolation
        var openName = typeof(TestInventory<>).FullName!;
        var typeRef = new TypeRef(openName, new[] { TypeRef.Simple("System.String") });

        var result = _resolver.Resolve(typeRef);

        Assert.That(result, Is.InstanceOf<TypeResolveResult.ConstraintViolation>());
    }

    [Test]
    public void Resolve_ConstraintSatisfied_InterfaceConstraint()
    {
        // TestSword : ITestItem → la contrainte est satisfaite
        var openName = typeof(TestInventory<>).FullName!;
        var typeRef = new TypeRef(openName, new[] { TypeRef.Simple(NameOf<TestSword>()) });

        var result = _resolver.Resolve(typeRef);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void Resolve_ConstraintViolation_ClassConstraint()
    {
        // TestBox<TItem, TLabel> where TItem : class
        // On passe TestValueItem (struct) → ConstraintViolation
        var openName = typeof(TestBox<,>).FullName!;
        var typeRef = new TypeRef(openName, new[]
        {
            TypeRef.Simple(NameOf<TestValueItem>()),
            TypeRef.Simple("System.String")
        });

        var result = _resolver.Resolve(typeRef);

        Assert.That(result, Is.InstanceOf<TypeResolveResult.ConstraintViolation>());
    }

    // -------------------------------------------------------------------------
    // Cache — un type résolu deux fois donne le même résultat
    // -------------------------------------------------------------------------

    [Test]
    public void Resolve_SameTypeTwice_ReturnsSameType()
    {
        var typeRef = TypeRef.Simple(NameOf<TestSword>());

        var result1 = _resolver.Resolve(typeRef);
        var result2 = _resolver.Resolve(typeRef);

        Assert.That(result1.IsSuccess, Is.True);
        Assert.That(result2.IsSuccess, Is.True);
        Assert.That(((TypeResolveResult.Success)result1).Type,
            Is.EqualTo(((TypeResolveResult.Success)result2).Type));
    }

    // -------------------------------------------------------------------------
    // RegisterAssembly — doublons ignorés
    // -------------------------------------------------------------------------

    [Test]
    public void RegisterAssembly_Duplicate_IsIgnoredSilently()
    {
        // Enregistrer deux fois le même assembly ne doit pas causer d'erreur
        // ni de comportement indéterminé.
        Assert.DoesNotThrow(() =>
        {
            _resolver.RegisterAssembly(_testAssembly);
            _resolver.RegisterAssembly(_testAssembly);
        });

        // La résolution doit toujours fonctionner correctement.
        var result = _resolver.Resolve(TypeRef.Simple(NameOf<TestSword>()));
        Assert.That(result.IsSuccess, Is.True);
    }
}