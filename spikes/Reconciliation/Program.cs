using Reconciliation;

var owner = new BuildOwner();
var root = owner.MountRoot(new App());

void Print(string label)
{
    Console.WriteLine($"== {label} ==");
    Console.WriteLine(root.Render());
    Console.WriteLine();
}

// Walks the retained Element tree to grab a live State (the spike's "GlobalKey").
static T? FindState<T>(Element element) where T : State
{
    if (element.State is T match)
        return match;
    foreach (var child in element.Children)
        if (FindState<T>(child) is { } found)
            return found;
    return null;
}

Print("initial");

// 1) The user "clicks" the counter.
FindState<CounterState>(root)!.Increment();
owner.FlushDirty();
Print("after counter increment (count -> 1)");

// 2) The PARENT rebuilds (a fresh Counter widget is created in App.Build()).
//    The decisive question: does the count survive?
FindState<AppState>(root)!.Tick();
owner.FlushDirty();
Print("after PARENT rebuild — count survives reconciliation");
