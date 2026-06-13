namespace HumbleEngine.Tests.Reactive;

/// <summary>
/// Unit tests for <see cref="ObservableList{T}"/> as a reactive source: its
/// structure is auto-tracked. Reading it inside an <see cref="Effect"/> (Count,
/// the indexer, enumeration) subscribes that effect, and any structural mutation
/// re-runs it — independently of, and in addition to, the exact Added/Removed
/// narration. The two mechanisms have different clients and coexist.
/// </summary>
public sealed class ObservableListTrackingTests
{
    [Test]
    public void Effect_ReadingCount_RerunsOnAdd()
    {
        var list = new ObservableList<string>();
        var counts = new List<int>();
        using var effect = new Effect(() => counts.Add(list.Count));

        list.Add("a");
        list.Add("b");

        Assert.That(counts, Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public void Effect_ReadingCount_RerunsOnRemoveAndClear()
    {
        var list = new ObservableList<string> { "a", "b", "c" };
        var counts = new List<int>();
        using var effect = new Effect(() => counts.Add(list.Count));

        list.RemoveAt(0); // 2
        list.Clear();     // N removals: 1, then 0

        Assert.That(counts, Is.EqualTo(new[] { 3, 2, 1, 0 }));
    }

    [Test]
    public void Effect_TracksThroughEnumeration()
    {
        var list = new ObservableList<int> { 1, 2 };
        var sums = new List<int>();
        using var effect = new Effect(() => sums.Add(list.Sum()));

        list.Add(3); // 6
        list.Add(4); // 10

        Assert.That(sums, Is.EqualTo(new[] { 3, 6, 10 }));
    }

    [Test]
    public void Effect_RerunsEvenWithNoNarrationSubscriber()
    {
        // The structure signal must fire whether or not anyone listens to
        // Added/Removed — the layout effect tracks structure without subscribing
        // to the exact narration. (Regression: Notify used to bail on a null handler.)
        var list = new ObservableList<string>();
        var runs = 0;
        using var effect = new Effect(() => { _ = list.Count; runs++; });

        list.Add("a");

        Assert.That(runs, Is.EqualTo(2));
    }

    [Test]
    public void Effect_DoesNotRerun_OnSameValueIndexerSet()
    {
        // An equal assignment narrates nothing and changes no structure.
        var list = new ObservableList<string> { "a" };
        var runs = 0;
        using var effect = new Effect(() => { _ = list.Count; runs++; });

        list[0] = "a";

        Assert.That(runs, Is.EqualTo(1)); // only the initial run
    }

    [Test]
    public void Effect_IndexerSet_RerunsTwice_RemoveThenAdd()
    {
        // A replace is remove + add: the eager, transient-tolerant philosophy of the
        // graph means two invalidations, not a coalesced one.
        var list = new ObservableList<string> { "old" };
        var runs = 0;
        using var effect = new Effect(() => { _ = list.Count; runs++; });

        list[0] = "new";

        Assert.That(runs, Is.EqualTo(3)); // initial, after remove, after add
    }

    [Test]
    public void StructureTracking_AndExactNarration_Coexist()
    {
        var list = new ObservableList<string>();
        var narration = new List<string>();
        list.Added += (i, item) => narration.Add($"add:{i}:{item}");
        var runs = 0;
        using var effect = new Effect(() => { _ = list.Count; runs++; });

        list.Add("a");

        Assert.That(narration, Is.EqualTo(new[] { "add:0:a" })); // exact narration intact
        Assert.That(runs, Is.EqualTo(2));                        // structure signal fired
    }
}
