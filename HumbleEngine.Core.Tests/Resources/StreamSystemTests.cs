using System.Text;
using HumbleEngine.Core.Resources;
using NUnit.Framework;

namespace HumbleEngine.Core.Tests.Resources;

// -------------------------------------------------------------------------
// Provider en mémoire — simule un fournisseur async sans dépendance réseau
// ni filesystem. Les données sont stockées dans un dictionnaire path → contenu.
// -------------------------------------------------------------------------

internal sealed class InMemoryStreamProvider : IStreamProvider
{
    private readonly Dictionary<string, string> _resources;

    public InMemoryStreamProvider(Dictionary<string, string> resources)
    {
        _resources = resources;
    }

    public Task<Stream> LoadAsync(string path)
    {
        if (!_resources.TryGetValue(path, out var content))
            throw new StreamNotFoundException($"mem://{path}", $"clé '{path}' absente");

        Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string path) =>
        Task.FromResult(_resources.ContainsKey(path));
}

[TestFixture]
internal sealed class StreamSystemTests
{
    // -------------------------------------------------------------------------
    // Enregistrement et résolution de schéma
    // -------------------------------------------------------------------------

    [Test]
    public async Task LoadAsync_RegisteredScheme_DelegatesToProvider()
    {
        var system = new StreamSystem();
        system.Register("mem", new InMemoryStreamProvider(
            new Dictionary<string, string> { ["scenes/player.hscene"] = "{}" }));

        await using var stream = await system.LoadAsync("mem://scenes/player.hscene");
        var content = await new StreamReader(stream).ReadToEndAsync();

        Assert.That(content, Is.EqualTo("{}"));
    }

    [Test]
    public void LoadAsync_UnregisteredScheme_ThrowsStreamNotFoundException()
    {
        var system = new StreamSystem();

        var ex = Assert.ThrowsAsync<StreamNotFoundException>(async () =>
            await system.LoadAsync("remote://cdn/ui.hscene"));

        Assert.That(ex!.Uri, Is.EqualTo("remote://cdn/ui.hscene"));
        Assert.That(ex.Message, Does.Contain("remote"));
    }

    [Test]
    public async Task LoadAsync_SchemeComparison_IsCaseInsensitive()
    {
        var system = new StreamSystem();
        system.Register("res", new InMemoryStreamProvider(
            new Dictionary<string, string> { ["a.hscene"] = "content" }));

        // Le schéma en majuscules doit être reconnu.
        Assert.DoesNotThrowAsync(async () =>
        {
            await using var stream = await system.LoadAsync("RES://a.hscene");
        });
    }

    [Test]
    public async Task Register_OverwritesExistingProvider()
    {
        var system = new StreamSystem();
        system.Register("mem", new InMemoryStreamProvider(
            new Dictionary<string, string> { ["file"] = "v1" }));
        system.Register("mem", new InMemoryStreamProvider(
            new Dictionary<string, string> { ["file"] = "v2" }));

        await using var stream = await system.LoadAsync("mem://file");
        var content = await new StreamReader(stream).ReadToEndAsync();

        Assert.That(content, Is.EqualTo("v2"));
    }

    [Test]
    public async Task Register_MultipleSchemes_EachResolvesIndependently()
    {
        var system = new StreamSystem();
        system.Register("local", new InMemoryStreamProvider(
            new Dictionary<string, string> { ["a"] = "local-content" }));
        system.Register("remote", new InMemoryStreamProvider(
            new Dictionary<string, string> { ["a"] = "remote-content" }));

        await using var localStream = await system.LoadAsync("local://a");
        await using var remoteStream = await system.LoadAsync("remote://a");

        Assert.That(await new StreamReader(localStream).ReadToEndAsync(), Is.EqualTo("local-content"));
        Assert.That(await new StreamReader(remoteStream).ReadToEndAsync(), Is.EqualTo("remote-content"));
    }

    // -------------------------------------------------------------------------
    // Parsing d'URI
    // -------------------------------------------------------------------------

    [Test]
    public void LoadAsync_UriWithoutSeparator_ThrowsStreamNotFoundException()
    {
        var system = new StreamSystem();

        Assert.ThrowsAsync<StreamNotFoundException>(async () =>
            await system.LoadAsync("not-a-valid-uri"));
    }

    [Test]
    public void LoadAsync_EmptyPath_ThrowsStreamNotFoundException()
    {
        var system = new StreamSystem();
        system.Register("res", new InMemoryStreamProvider(new Dictionary<string, string>()));

        Assert.ThrowsAsync<StreamNotFoundException>(async () =>
            await system.LoadAsync("res://"));
    }

    [Test]
    public void LoadAsync_EmptyUri_ThrowsArgumentException()
    {
        var system = new StreamSystem();

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await system.LoadAsync(""));
    }

    // -------------------------------------------------------------------------
    // ExistsAsync
    // -------------------------------------------------------------------------

    [Test]
    public async Task ExistsAsync_PresentResource_ReturnsTrue()
    {
        var system = new StreamSystem();
        system.Register("mem", new InMemoryStreamProvider(
            new Dictionary<string, string> { ["scene.hscene"] = "{}" }));

        Assert.That(await system.ExistsAsync("mem://scene.hscene"), Is.True);
    }

    [Test]
    public async Task ExistsAsync_AbsentResource_ReturnsFalse()
    {
        var system = new StreamSystem();
        system.Register("mem", new InMemoryStreamProvider(new Dictionary<string, string>()));

        Assert.That(await system.ExistsAsync("mem://missing.hscene"), Is.False);
    }

    [Test]
    public async Task ExistsAsync_UnregisteredScheme_ReturnsFalse()
    {
        // ExistsAsync ne lève pas d'exception pour un schéma inconnu.
        var system = new StreamSystem();

        Assert.That(await system.ExistsAsync("remote://anything"), Is.False);
    }

    // -------------------------------------------------------------------------
    // Register — validation des arguments
    // -------------------------------------------------------------------------

    [Test]
    public void Register_NullProvider_Throws()
    {
        var system = new StreamSystem();

        Assert.Throws<ArgumentNullException>(() =>
            system.Register("res", null!));
    }

    [Test]
    public void Register_EmptyScheme_Throws()
    {
        var system = new StreamSystem();

        Assert.Throws<ArgumentException>(() =>
            system.Register("", new InMemoryStreamProvider(new Dictionary<string, string>())));
    }
}

// -------------------------------------------------------------------------
// Tests du FileSystemStreamProvider — on crée un dossier temporaire
// pour simuler la racine d'un projet.
// -------------------------------------------------------------------------

[TestFixture]
internal sealed class FileSystemStreamProviderTests
{
    private string _tempRoot = null!;

    [SetUp]
    public void SetUp()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempRoot);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    [Test]
    public async Task LoadAsync_ExistingFile_ReturnsContent()
    {
        File.WriteAllText(Path.Combine(_tempRoot, "scene.hscene"), "{\"key\":\"value\"}");
        var provider = new FileSystemStreamProvider(_tempRoot);

        await using var stream = await provider.LoadAsync("scene.hscene");
        var content = await new StreamReader(stream).ReadToEndAsync();

        Assert.That(content, Is.EqualTo("{\"key\":\"value\"}"));
    }

    [Test]
    public async Task LoadAsync_NestedPath_ResolvesCorrectly()
    {
        Directory.CreateDirectory(Path.Combine(_tempRoot, "scenes"));
        File.WriteAllText(Path.Combine(_tempRoot, "scenes", "player.hscene"), "player-json");
        var provider = new FileSystemStreamProvider(_tempRoot);

        await using var stream = await provider.LoadAsync("scenes/player.hscene");
        var content = await new StreamReader(stream).ReadToEndAsync();

        Assert.That(content, Is.EqualTo("player-json"));
    }

    [Test]
    public void LoadAsync_MissingFile_ThrowsStreamNotFoundException()
    {
        var provider = new FileSystemStreamProvider(_tempRoot);

        Assert.ThrowsAsync<StreamNotFoundException>(async () =>
            await provider.LoadAsync("does/not/exist.hscene"));
    }

    [Test]
    public void LoadAsync_PathTraversal_ThrowsStreamNotFoundException()
    {
        var provider = new FileSystemStreamProvider(_tempRoot);

        Assert.ThrowsAsync<StreamNotFoundException>(async () =>
            await provider.LoadAsync("../../etc/passwd"));
    }

    [Test]
    public async Task ExistsAsync_ExistingFile_ReturnsTrue()
    {
        File.WriteAllText(Path.Combine(_tempRoot, "exists.hscene"), "{}");
        var provider = new FileSystemStreamProvider(_tempRoot);

        Assert.That(await provider.ExistsAsync("exists.hscene"), Is.True);
    }

    [Test]
    public async Task ExistsAsync_MissingFile_ReturnsFalse()
    {
        var provider = new FileSystemStreamProvider(_tempRoot);

        Assert.That(await provider.ExistsAsync("missing.hscene"), Is.False);
    }
}