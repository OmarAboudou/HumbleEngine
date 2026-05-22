using NUnit.Framework;

namespace HumbleEngine.Tests;

[TestFixture]
public class ReactiveCollectionTests
{
    [Test]
    public void Add_EmitsItemAdded()
    {
        var col = new ReactiveCollection<string>();
        (int index, string item) received = (-1, "");
        col.ItemAdded.Connect(e => received = e);

        col.Add("hello");

        Assert.That(received.index, Is.EqualTo(0));
        Assert.That(received.item, Is.EqualTo("hello"));
    }

    [Test]
    public void Add_UpdatesCount()
    {
        var col = new ReactiveCollection<int>();
        col.Add(1);
        col.Add(2);

        Assert.That(col.Count, Is.EqualTo(2));
    }

    [Test]
    public void RemoveAt_EmitsItemRemoved()
    {
        var col = new ReactiveCollection<string>();
        col.Add("a");
        col.Add("b");
        (int index, string item) received = (-1, "");
        col.ItemRemoved.Connect(e => received = e);

        col.RemoveAt(0);

        Assert.That(received.index, Is.EqualTo(0));
        Assert.That(received.item, Is.EqualTo("a"));
    }

    [Test]
    public void Remove_ReturnsFalseIfNotFound()
    {
        var col = new ReactiveCollection<string>();
        col.Add("a");

        bool result = col.Remove("z");

        Assert.That(result, Is.False);
    }

    [Test]
    public void Clear_EmitsReset()
    {
        var col = new ReactiveCollection<int>();
        col.Add(1);
        col.Add(2);
        bool resetFired = false;
        col.Cleared.Connect(() => resetFired = true);

        col.Clear();

        Assert.That(resetFired, Is.True);
        Assert.That(col.Count, Is.EqualTo(0));
    }

    [Test]
    public void Indexer_ReturnsCorrectItem()
    {
        var col = new ReactiveCollection<string>();
        col.Add("x");
        col.Add("y");

        Assert.That(col[1], Is.EqualTo("y"));
    }

    [Test]
    public void Insert_EmitsItemAddedAtCorrectIndex()
    {
        var col = new ReactiveCollection<string>();
        col.Add("a");
        col.Add("c");
        (int index, string item) received = (-1, "");
        col.ItemAdded.Connect(e => received = e);

        col.Insert(1, "b");

        Assert.That(received.index, Is.EqualTo(1));
        Assert.That(received.item, Is.EqualTo("b"));
        Assert.That(col[1], Is.EqualTo("b"));
    }

    [Test]
    public void Changed_FiredOnAdd()
    {
        var col = new ReactiveCollection<int>();
        int callCount = 0;
        col.Changed.Connect(() => callCount++);

        col.Add(1);

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Changed_FiredOnRemove()
    {
        var col = new ReactiveCollection<int>();
        col.Add(1);
        int callCount = 0;
        col.Changed.Connect(() => callCount++);

        col.RemoveAt(0);

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Changed_FiredOnClear()
    {
        var col = new ReactiveCollection<int>();
        col.Add(1);
        int callCount = 0;
        col.Changed.Connect(() => callCount++);

        col.Clear();

        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Changed_FiredOncePerOperation()
    {
        var col = new ReactiveCollection<int>();
        int callCount = 0;
        col.Changed.Connect(() => callCount++);

        col.Add(1);
        col.Add(2);
        col.RemoveAt(0);

        Assert.That(callCount, Is.EqualTo(3));
    }
}
