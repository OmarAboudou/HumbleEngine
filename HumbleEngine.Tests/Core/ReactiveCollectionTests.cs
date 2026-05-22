using NUnit.Framework;

namespace HumbleEngine.Tests;

[TestFixture]
public class ReactiveCollectionTests
{
    [Test]
    public void Add_EmitsItemAdded()
    {
        var col = new ReactiveCollection<string>();
        int receivedIndex = -1;
        string receivedItem = "";
        col.ItemAdded.Connect((index, item) => { receivedIndex = index; receivedItem = item; });

        col.Add("hello");

        Assert.That(receivedIndex, Is.EqualTo(0));
        Assert.That(receivedItem, Is.EqualTo("hello"));
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
        int receivedIndex = -1;
        string receivedItem = "";
        col.ItemRemoved.Connect((index, item) => { receivedIndex = index; receivedItem = item; });

        col.RemoveAt(0);

        Assert.That(receivedIndex, Is.EqualTo(0));
        Assert.That(receivedItem, Is.EqualTo("a"));
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
    public void Clear_EmitsClearedSignal()
    {
        var col = new ReactiveCollection<int>();
        col.Add(1);
        col.Add(2);
        bool clearedFired = false;
        col.Cleared.Connect(() => clearedFired = true);

        col.Clear();

        Assert.That(clearedFired, Is.True);
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
        int receivedIndex = -1;
        string receivedItem = "";
        col.ItemAdded.Connect((index, item) => { receivedIndex = index; receivedItem = item; });

        col.Insert(1, "b");

        Assert.That(receivedIndex, Is.EqualTo(1));
        Assert.That(receivedItem, Is.EqualTo("b"));
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
