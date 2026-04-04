namespace HumbleEngine.Core.Tests;

[HumbleType("a1b2c3d4-e5f6-7890-abcd-ef1234567890")]
class RegisteredType;

[TestFixture]
public class HumbleTypeTests
{
    [OneTimeSetUp]
    public void RegisterTypes()
    {
        Services.HumbleTypeRegistry.Register(typeof(RegisteredType));
    }

    [Test]
    public void Resolve_ReturnsCorrectType_WhenTypeIsRegistered()
    {
        var humbleType = new HumbleType(Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));

        Type resolved = humbleType.Resolve();

        Assert.That(resolved, Is.EqualTo(typeof(RegisteredType)));
    }

    [Test]
    public void Resolve_Throws_WhenTypeIsNotRegistered()
    {
        var unknownId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var humbleType = new HumbleType(unknownId);

        Assert.Throws<ArgumentException>(() => humbleType.Resolve());
    }
}