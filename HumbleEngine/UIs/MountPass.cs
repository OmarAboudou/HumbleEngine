namespace HumbleEngine;

internal static class MountPass
{
    internal static void Mount(Widget widget)
    {
        if (widget is CompositeWidget composite && composite.IsDirty)
        {
            composite.BuiltSubTree = composite.Build();
            composite.IsDirty = false;
        }

        widget.MountedChildren.Clear();
        foreach (Widget child in widget.GetChildren())
        {
            widget.MountedChildren.Add(child);
            child.Parent = widget;
            Mount(child);
        }
    }
}
