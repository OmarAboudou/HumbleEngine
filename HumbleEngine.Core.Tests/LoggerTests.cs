namespace HumbleEngine.Core.Tests;

class TestSink : ILogSink
{
    public List<LogEntry> Entries { get; } = [];
    public void Write<TChannel>(LogEntry<TChannel> logEntry) where TChannel : ILogChannel
        => Entries.Add(logEntry);
}

class TestChannel : ILogChannel
{
    public static string ChannelName => "TEST";
}

class TestChannel2 : ILogChannel
{
    public static string ChannelName => "TEST2";
}

[TestFixture]
public class LoggerTests
{
    private Logger _logger = null!;
    private TestSink _sink = null!;

    [SetUp]
    public void SetUp()
    {
        _sink = new TestSink();
        _logger = new Logger();
        _logger.AddSink(_sink);
        _logger.SetDefaultLevel(LogLevel.TRACE);
    }

    [Test]
    public void Warning_WritesEntryToSink()
    {
        _logger.Warning<TestChannel>("test message");

        Assert.That(_sink.Entries, Has.Count.EqualTo(1));
        Assert.That(_sink.Entries[0].Level, Is.EqualTo(LogLevel.WARNING));
        Assert.That(_sink.Entries[0].Message, Is.EqualTo("test message"));
    }

    [Test]
    public void SetDefaultLevel_FiltersBelowLevel()
    {
        _logger.SetDefaultLevel(Logger.LevelCap);

        _logger.Debug<TestChannel>("filtered");
        _logger.Info<TestChannel>("filtered");

        Assert.That(_sink.Entries, Is.Empty);
    }

    [Test]
    public void SetDefaultLevel_AllowsAtOrAboveLevel()
    {
        _logger.SetDefaultLevel(Logger.LevelCap);

        _logger.Warning<TestChannel>("allowed");
        _logger.Error<TestChannel>("allowed");

        Assert.That(_sink.Entries, Has.Count.EqualTo(2));
    }

    [Test]
    public void SetChannelLevel_FiltersBelowChannelLevel()
    {
        _logger.SetChannelLevel<TestChannel>(LogLevel.WARNING);

        _logger.Debug<TestChannel>("filtered");
        _logger.Info<TestChannel>("filtered");

        Assert.That(_sink.Entries, Is.Empty);
    }

    [Test]
    public void SetChannelLevel_AllowsAtOrAboveChannelLevel()
    {
        _logger.SetChannelLevel<TestChannel>(LogLevel.WARNING);

        _logger.Warning<TestChannel>("allowed");
        _logger.Error<TestChannel>("allowed");

        Assert.That(_sink.Entries, Has.Count.EqualTo(2));
    }

    [Test]
    public void SetChannelLevel_DoesNotAffectOtherChannels()
    {
        _logger.SetChannelLevel<TestChannel>(LogLevel.WARNING);

        _logger.Debug<TestChannel2>("allowed");

        Assert.That(_sink.Entries, Has.Count.EqualTo(1));
    }

    [Test]
    public void ClearChannelLevel_FallsBackToDefaultLevel()
    {
        _logger.SetChannelLevel<TestChannel>(LogLevel.WARNING);
        _logger.ClearChannelLevel<TestChannel>();

        _logger.Debug<TestChannel>("allowed");

        Assert.That(_sink.Entries, Has.Count.EqualTo(1));
    }

    [Test]
    public void SetDefaultLevel_CapsAtLevelCap_WhenLevelExceedsCap()
    {
        _logger.SetDefaultLevel(LogLevel.FATAL);

        Assert.That(_logger.DefaultLevel, Is.EqualTo(Logger.LevelCap));
    }

    [Test]
    public void SetDefaultLevel_EmitsWarning_WhenLevelExceedsCap()
    {
        _logger.SetDefaultLevel(LogLevel.FATAL);

        Assert.That(_sink.Entries, Has.Count.EqualTo(1));
        Assert.That(_sink.Entries[0].Level, Is.EqualTo(LogLevel.WARNING));
    }

    [Test]
    public void SetChannelLevel_CapsAtLevelCap_WhenLevelExceedsCap()
    {
        _logger.SetChannelLevel<TestChannel>(LogLevel.FATAL);

        _logger.Debug<TestChannel>("filtered");

        Assert.That(_sink.Entries, Has.Count.EqualTo(1)); // only the cap warning, Debug was filtered
    }

    [Test]
    public void SetChannelLevel_EmitsWarning_WhenLevelExceedsCap()
    {
        _logger.SetChannelLevel<TestChannel>(LogLevel.FATAL);

        Assert.That(_sink.Entries, Has.Count.EqualTo(1));
        Assert.That(_sink.Entries[0].Level, Is.EqualTo(LogLevel.WARNING));
    }
}