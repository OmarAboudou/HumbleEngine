namespace HumbleEngine.Tests.Reactive;

/// <summary>
/// Unit tests for <see cref="ReactiveList{T}"/>: exact change narration on two
/// events, no reset (Clear = N removals from the end, replace = remove + add),
/// duplicates, and the in-handler mutation guard.
/// </summary>
public sealed class ReactiveListTests
{
    /// <summary>Records every event as "verb:index:item" to assert exact narration.</summary>
    private static List<string> Record(ReactiveList<string> list)
    {
        var log = new List<string>();
        list.Added += (i, item) => log.Add($"add:{i}:{item}");
        list.Removed += (i, item) => log.Add($"rem:{i}:{item}");
        return log;
    }

    [Test]
    public void NewList_IsEmpty()
    {
        var list = new ReactiveList<string>();

        Assert.That(list.Count, Is.EqualTo(0));
        Assert.That(list, Is.Empty);
    }

    [Test]
    public void Add_Appends_AndNarratesIndexAndItem()
    {
        var list = new ReactiveList<string>();
        var log = Record(list);

        list.Add("a");
        list.Add("b");

        Assert.That(list, Is.EqualTo(new[] { "a", "b" }));
        Assert.That(log, Is.EqualTo(new[] { "add:0:a", "add:1:b" }));
    }

    [Test]
    public void CollectionInitializer_Works()
    {
        var list = new ReactiveList<int> { 1, 2, 3 };

        Assert.That(list, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Insert_ShiftsLaterItems()
    {
        var list = new ReactiveList<string> { "a", "c" };
        var log = Record(list);

        list.Insert(1, "b");

        Assert.That(list, Is.EqualTo(new[] { "a", "b", "c" }));
        Assert.That(log, Is.EqualTo(new[] { "add:1:b" }));
    }

    [Test]
    public void Remove_NarratesOldIndex_AndCarriesTheItem()
    {
        var list = new ReactiveList<string> { "a", "b", "c" };
        var log = Record(list);

        Assert.That(list.Remove("b"), Is.True);

        Assert.That(list, Is.EqualTo(new[] { "a", "c" }));
        Assert.That(log, Is.EqualTo(new[] { "rem:1:b" }));
    }

    [Test]
    public void Remove_AbsentItem_ReturnsFalse_NoEvent()
    {
        var list = new ReactiveList<string> { "a" };
        var log = Record(list);

        Assert.That(list.Remove("z"), Is.False);
        Assert.That(log, Is.Empty);
    }

    [Test]
    public void RemoveAt_RemovesByPosition()
    {
        var list = new ReactiveList<string> { "a", "b" };
        var log = Record(list);

        list.RemoveAt(0);

        Assert.That(list, Is.EqualTo(new[] { "b" }));
        Assert.That(log, Is.EqualTo(new[] { "rem:0:a" }));
    }

    [Test]
    public void Duplicates_AreAllowed_RemoveTakesFirstOccurrence()
    {
        var list = new ReactiveList<string> { "x", "y", "x" };
        var log = Record(list);

        list.Remove("x");

        Assert.That(list, Is.EqualTo(new[] { "y", "x" }));
        Assert.That(log, Is.EqualTo(new[] { "rem:0:x" }));
    }

    [Test]
    public void IndexerSet_IsRemoveThenAdd_AtTheSameIndex()
    {
        var list = new ReactiveList<string> { "a", "old", "c" };
        var log = Record(list);

        list[1] = "new";

        Assert.That(list, Is.EqualTo(new[] { "a", "new", "c" }));
        Assert.That(log, Is.EqualTo(new[] { "rem:1:old", "add:1:new" }));
    }

    [Test]
    public void IndexerSet_SameValue_DoesNothing()
    {
        var list = new ReactiveList<string> { "a" };
        var log = Record(list);

        list[0] = "a";

        Assert.That(log, Is.Empty);
    }

    [Test]
    public void IndexerSet_ListIsConsistent_BetweenTheTwoEvents()
    {
        var list = new ReactiveList<string> { "old" };
        var countDuringRemoved = -1;
        list.Removed += (_, _) => countDuringRemoved = list.Count;

        list[0] = "new";

        Assert.That(countDuringRemoved, Is.EqualTo(0));
    }

    [Test]
    public void Clear_IsNRemovals_FromTheEndTowardsTheStart()
    {
        var list = new ReactiveList<string> { "a", "b", "c" };
        var log = Record(list);

        list.Clear();

        Assert.That(list, Is.Empty);
        Assert.That(log, Is.EqualTo(new[] { "rem:2:c", "rem:1:b", "rem:0:a" }));
    }

    [Test]
    public void MutatingFromOwnHandler_Throws()
    {
        var list = new ReactiveList<string>();
        list.Added += (_, _) => list.Add("echo");

        Assert.Throws<InvalidOperationException>(() => list.Add("a"));
    }

    [Test]
    public void MutatingAnotherList_FromAHandler_IsAllowed()
    {
        var source = new ReactiveList<string>();
        var mirror = new ReactiveList<string>();
        source.Added += (i, item) => mirror.Insert(i, item);
        source.Removed += (i, _) => mirror.RemoveAt(i);

        source.Add("a");
        source.Add("b");
        source.RemoveAt(0);

        Assert.That(mirror, Is.EqualTo(new[] { "b" }));
    }

    [Test]
    public void ReadOnlyView_ExposesReadsAndEvents()
    {
        var list = new ReactiveList<string> { "a" };
        IReadOnlyReactiveList<string> view = list;
        var seen = "";
        view.Added += (_, item) => seen = item;

        list.Add("b");

        Assert.That(view.Count, Is.EqualTo(2));
        Assert.That(view[1], Is.EqualTo("b"));
        Assert.That(seen, Is.EqualTo("b"));
    }
}
