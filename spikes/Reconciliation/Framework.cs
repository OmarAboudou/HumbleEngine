namespace Reconciliation;

// A minimal, self-contained reproduction of Flutter's Widget/Element/State model,
// to FEEL the reconciliation architecture (not the rendering). The decisive thing
// to watch: a Stateful widget's State survives a parent rebuild, because the
// retained Element (and its State) is reused when the new widget matches the old.

/// <summary>Optional identity used by reconciliation, on top of the runtime type.</summary>
public abstract record Key;

/// <summary>A simple value-based key (the spike's stand-in for Flutter's ValueKey).</summary>
public sealed record ValueKey<T>(T Value) : Key;

/// <summary>Immutable description of a piece of UI. Cheap to recreate every build.</summary>
public abstract class Widget
{
    public Key? Key { get; init; }
}

/// <summary>A leaf that "renders" a string — our stand-in for a RenderObject.</summary>
public sealed class Text(string value) : Widget
{
    public string Value { get; } = value;
}

/// <summary>A widget with a fixed list of children (multi-child).</summary>
public sealed class Column(params Widget[] children) : Widget
{
    public IReadOnlyList<Widget> Children { get; } = children;
}

/// <summary>Builds exactly one child from immutable inputs — no state of its own.</summary>
public abstract class StatelessWidget : Widget
{
    public abstract Widget Build();
}

/// <summary>
/// Creates a <see cref="State"/> that persists across rebuilds. This persistence —
/// keeping the State while the Widget description is thrown away and recreated — is
/// the whole point of reconciliation.
/// </summary>
public abstract class StatefulWidget : Widget
{
    public abstract State CreateState();
}

/// <summary>The persistent state behind a <see cref="StatefulWidget"/>.</summary>
public abstract class State
{
    internal Element Element = null!;

    /// <summary>Describes the UI for the current state.</summary>
    public abstract Widget Build();

    /// <summary>Mutates state and schedules a rebuild of this element's subtree.</summary>
    protected void SetState(Action change)
    {
        change();
        Element.MarkDirty();
    }
}
