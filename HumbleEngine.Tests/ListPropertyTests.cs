namespace HumbleEngine.Tests;

[TestFixture]
public class ListPropertyTests
{
    [Test]
    public void Add_AppendsItem()
    {
        var list = new ListProperty<int>();
        list.Add(1);
        list.Add(2);
        Assert.That(list, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Add_FiresAddedSignalWithCorrectIndex()
    {
        var list = new ListProperty<string>();
        list.Add("a");
        (string item, int index) received = default;
        list.Added.Connect(e => received = e);
        list.Add("b");
        Assert.That(received, Is.EqualTo(("b", 1)));
    }

    [Test]
    public void Insert_InsertsAtCorrectPosition()
    {
        var list = new ListProperty<int> { 1, 3 };
        list.Insert(1, 2);
        Assert.That(list, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Remove_RemovesItem_ReturnsTrue()
    {
        var list = new ListProperty<int> { 1, 2, 3 };
        bool result = list.Remove(2);
        Assert.That(result, Is.True);
        Assert.That(list, Is.EqualTo(new[] { 1, 3 }));
    }

    [Test]
    public void Remove_AbsentItem_ReturnsFalse()
    {
        var list = new ListProperty<int> { 1, 2 };
        Assert.That(list.Remove(99), Is.False);
    }

    [Test]
    public void RemoveAt_FiresRemovedSignalWithCorrectItemAndIndex()
    {
        var list = new ListProperty<string> { "a", "b", "c" };
        (string item, int index) received = default;
        list.Removed.Connect(e => received = e);
        list.RemoveAt(1);
        Assert.That(received, Is.EqualTo(("b", 1)));
    }

    [Test]
    public void Clear_RemovesAllItems()
    {
        var list = new ListProperty<int> { 1, 2, 3 };
        list.Clear();
        Assert.That(list.Count, Is.EqualTo(0));
    }

    [Test]
    public void Indexer_Set_ReplacesItem()
    {
        var list = new ListProperty<int> { 1, 2, 3 };
        list[1] = 99;
        Assert.That(list[1], Is.EqualTo(99));
    }

    [Test]
    public void Indexer_SetSameValue_DoesNotFireSignals()
    {
        var list = new ListProperty<int> { 5 };
        int calls = 0;
        list.Added.Connect(_ => calls++);
        list.Removed.Connect(_ => calls++);
        list[0] = 5;
        Assert.That(calls, Is.EqualTo(0));
    }

    [Test]
    public void AsReadOnly_ReflectsListChanges()
    {
        var list = new ListProperty<int> { 1, 2 };
        var ro = list.AsReadOnly();
        list.Add(3);
        Assert.That(ro, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void AsReadOnly_Added_FiresWhenItemAdded()
    {
        var list = new ListProperty<int>();
        var ro = list.AsReadOnly();
        (int item, int index) received = default;
        ro.Added.Connect(e => received = e);
        list.Add(7);
        Assert.That(received, Is.EqualTo((7, 0)));
    }

    [Test]
    public void AsReadOnly_Removed_FiresWhenItemRemoved()
    {
        var list = new ListProperty<int> { 10, 20 };
        var ro = list.AsReadOnly();
        (int item, int index) received = default;
        ro.Removed.Connect(e => received = e);
        list.RemoveAt(0);
        Assert.That(received, Is.EqualTo((10, 0)));
    }

    [Test]
    public void AsReadOnly_ReturnsSameInstance()
    {
        var list = new ListProperty<int>();
        Assert.That(list.AsReadOnly(), Is.SameAs(list.AsReadOnly()));
    }
}
