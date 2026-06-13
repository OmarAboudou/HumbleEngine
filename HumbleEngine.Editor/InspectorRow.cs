namespace HumbleEngine;

/// <summary>
/// One row in the inspector: the property's name on the left and a widget on the
/// right, chosen by the value's <see cref="IObservableValue.ValueType"/>:
/// <list type="bullet">
///   <item><c>string</c> + writable → <see cref="TextField"/> with a two-way binding.</item>
///   <item><c>bool</c>   + writable → clickable toggle label ("✓" / "✗").</item>
///   <item><c>float</c>  + writable → <see cref="TextField"/> with parse/format
///     (display-only when the field does not have focus, to avoid fighting the user's input).</item>
///   <item>anything else / read-only → <see cref="Label"/> showing <c>Value.ToString()</c>,
///     updated reactively.</item>
/// </list>
/// <para>
/// The row lays itself out manually (the same workaround as <see cref="HierarchyRow"/>):
/// name label on the left, widget on the right, total size reported so a parent
/// <see cref="Column"/> can stack the rows.
/// </para>
/// </summary>
public sealed class InspectorRow : UINode
{
    private const float LabelWidth  = 110f;
    private const float WidgetWidth = 160f;
    private const float RowHeight   = 22f;
    private const float Padding     = 4f;

    private static readonly Vector4 ColorLabel = new(0.7f,  0.7f,  0.75f, 1f);
    private static readonly Vector4 ColorTrue  = new(0.35f, 0.85f, 0.45f, 1f);
    private static readonly Vector4 ColorFalse = new(0.8f,  0.35f, 0.35f, 1f);

    private readonly Label   _nameLabel;
    private readonly UINode  _widget;

    /// <summary>Builds a row for <paramref name="prop"/>.</summary>
    public InspectorRow(NodeInspector.InspectableProperty prop)
    {
        _nameLabel = new Label
        {
            Hittable = false,
            Name     = "PropName",
        };
        _nameLabel.Text.Value  = prop.Name;
        _nameLabel.Color.Value = ColorLabel;
        Attach(_nameLabel);

        _widget = BuildWidget(prop.Value);
        Attach(_widget);

        CreateEffect(Layout);
    }

    // ── Widget factory ────────────────────────────────────────────────────────

    private UINode BuildWidget(IObservableValue value)
    {
        // string — two-way when writable, read-only label otherwise.
        if (value is Property<string> stringProp)
        {
            var field = new TextField { Name = "StringWidget" };
            field.Size.Value = new Vector2(WidgetWidth, RowHeight);
            field.Text.BindTwoWayFrom(stringProp);
            return field;
        }
        if (value is IObservableValue<string> stringObs)
            return ReadOnlyLabel(() => stringObs.Value ?? string.Empty);

        // bool — toggle button when writable.
        if (value is Property<bool> boolProp)
            return BoolToggle(boolProp);
        if (value is IObservableValue<bool> boolObs)
            return ReadOnlyLabel(() => boolObs.Value ? "✓" : "✗");

        // float — TextField with parse/format when writable.
        if (value is Property<float> floatProp)
            return FloatField(floatProp);
        if (value is IObservableValue<float> floatObs)
            return ReadOnlyLabel(() => floatObs.Value.ToString("G6"));

        // Fallback: any IObservableValue — read-only stringified.
        return ReadOnlyLabel(() => value.Value?.ToString() ?? "(null)");
    }

    /// <summary>
    /// A <see cref="Label"/> that refreshes via an <see cref="Effect"/> whenever
    /// the reactive getter <paramref name="getText"/> changes.
    /// </summary>
    private Label ReadOnlyLabel(Func<string> getText)
    {
        var label = new Label { Hittable = false, Name = "ReadOnlyWidget" };
        CreateEffect(() => label.Text.Value = getText());
        return label;
    }

    /// <summary>
    /// A <see cref="Panel"/> acting as a toggle button for a <c>bool</c>
    /// <see cref="Property{T}"/>: background colour follows the value, click flips it.
    /// </summary>
    private Panel BoolToggle(Property<bool> prop)
    {
        var toggle = new BoolToggleWidget(prop) { Name = "BoolWidget" };
        toggle.Size.Value = new Vector2(WidgetWidth, RowHeight);
        return toggle;
    }

    /// <summary>
    /// A <see cref="TextField"/> wired to a <c>float</c> <see cref="Property{T}"/>:
    /// the field shows the formatted value when idle; on any text change the value is
    /// parsed back (invalid input is silently dropped — the field will snap back on
    /// the next external change).
    /// </summary>
    private TextField FloatField(Property<float> floatProp)
    {
        var field = new TextField { Name = "FloatWidget" };
        field.Size.Value = new Vector2(WidgetWidth, RowHeight);

        // Source → display: use Changed event (not an Effect) to avoid the
        // display overwriting the user's in-progress input on every keystroke.
        floatProp.Changed += v => field.Text.Value = v.ToString("G6");
        field.Text.Value = floatProp.Value.ToString("G6"); // initial sync

        // Display → source: parse on every text change; drop invalid strings.
        field.Text.Changed += text =>
        {
            if (float.TryParse(text,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var parsed))
                floatProp.Value = parsed;
        };
        return field;
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Places the name label and the widget side by side and reports the row's size.
    /// Runs inside an <see cref="Effect"/> so the row resizes if the widget changes.
    /// </summary>
    private void Layout()
    {
        _nameLabel.Position.Value = new Vector2(Padding, (RowHeight - _nameLabel.Size.Value.Y) / 2f);
        _widget.Position.Value    = new Vector2(LabelWidth, (RowHeight - _widget.Size.Value.Y) / 2f);
        Size.Value                = new Vector2(LabelWidth + WidgetWidth + Padding, RowHeight);
    }
}

// ── Inner helper node ─────────────────────────────────────────────────────────

/// <summary>
/// A small panel that shows "✓" or "✗" and flips the <see cref="Property{T}"/>
/// on a left click. Declared alongside <see cref="InspectorRow"/> — it has no
/// use outside the inspector.
/// </summary>
file sealed class BoolToggleWidget : Panel
{
    private static readonly Vector4 BgTrue  = new(0.20f, 0.55f, 0.25f, 1f);
    private static readonly Vector4 BgFalse = new(0.40f, 0.18f, 0.18f, 1f);

    private readonly Property<bool> _prop;
    private readonly Label          _mark;

    internal BoolToggleWidget(Property<bool> prop)
    {
        _prop = prop;
        _mark = new Label { Hittable = false };
        Attach(_mark);

        CreateEffect(() =>
        {
            var v = _prop.Value;
            Color.Value      = v ? BgTrue : BgFalse;
            _mark.Text.Value = v ? "✓" : "✗";
            _mark.Position.Value = new Vector2(4f, 0f);
        });
    }

    /// <inheritdoc />
    protected override bool OnInput(InputEvent inputEvent)
    {
        if (inputEvent is PointerPressed { Button: PointerButton.Left })
        {
            _prop.Value = !_prop.Value;
            return true;
        }
        return false;
    }
}
