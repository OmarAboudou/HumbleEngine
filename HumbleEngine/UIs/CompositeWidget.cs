namespace HumbleEngine;

public abstract record CompositeWidget : Widget
{
    internal bool IsDirty { get; set; } = true;

    internal Widget? BuiltSubTree { get; set; }

    internal override IReadOnlyList<Widget> GetChildren() => BuiltSubTree is not null ? [BuiltSubTree] : [];

    public override void Layout(BoxConstraints constraints)
    {
        if (MountedChildren.Count > 0)
            MountedChildren[0].Layout(constraints);
    }

    internal override Size GetSize() =>
        MountedChildren.Count > 0 ? MountedChildren[0].GetSize() : new Size(0f, 0f);

    public abstract Widget Build();

    protected Property<T> CreateCompositeProperty<T>(T initialValue)
    {
        Property<T> property = CreatePublicProperty(initialValue);
        property.Connect(_ => IsDirty = true);
        return property;
    }

    protected void ConnectCompositeProperty<T>(Property<T> property)
    {
        property.Connect(_ => IsDirty = true);
    }

    protected ListProperty<T> CreateCompositeListProperty<T>()
    {
        ListProperty<T> listProperty = CreatePublicListProperty<T>();
        ConnectCompositeListProperty(listProperty);
        return listProperty;
    }

    protected void ConnectCompositeListProperty<T>(ListProperty<T> listProperty)
    {
        listProperty.ConnectAddedElement((_, _) => IsDirty = true);
        listProperty.ConnectRemovedElement((_, _) => IsDirty = true);
    }
}