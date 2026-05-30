namespace HumbleEngine;

public partial record SizedBox : PrimitiveSingleChildWidget<Widget>
{
    [PrimitiveWidgetProperty(WidgetRefreshFlag.LAYOUT)]
    public partial Property<float?> Width { get; init; }

    [PrimitiveWidgetProperty(WidgetRefreshFlag.LAYOUT)]
    public partial Property<float?> Height { get; init; }

    public override void Layout(BoxConstraints constraints)
    {
        // Notre taille cible : dimension explicite clampée aux contraintes parentes,
        // ou le minimum parent si la dimension n'est pas spécifiée.
        float targetWidth  = Width.Value  is { } w ? constraints.WidthConstraints.Constrain(w)  : constraints.MinWidth;
        float targetHeight = Height.Value is { } h ? constraints.HeightConstraints.Constrain(h) : constraints.MinHeight;

        if (Child.Value is PrimitiveWidget child)
        {
            // Contraintes pour l'enfant : tight sur les axes qu'on fixe,
            // contraintes parentes telles quelles sur les axes libres.
            BoxConstraints childConstraints = new(
                Width.Value  is not null ? LengthConstraints.Tight(targetWidth)  : constraints.WidthConstraints,
                Height.Value is not null ? LengthConstraints.Tight(targetHeight) : constraints.HeightConstraints
            );

            child.Layout(childConstraints);

            // Sur les axes libres, on s'adapte à l'enfant.
            if (Width.Value  is null) targetWidth  = child.Size.Value.Width;
            if (Height.Value is null) targetHeight = child.Size.Value.Height;
        }

        Size.Value = new Size(targetWidth, targetHeight);
    }
}
