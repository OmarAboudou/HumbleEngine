namespace HumbleEngine.Core;

public partial record  Column : LinearLayout<Widget>
{
    [WidgetProperty(WidgetRefreshFlag.LAYOUT)]
    public partial Property<bool> ExpandVertically { get; init; }
    
    protected override LayoutResult PerformLayout(BoxConstraints boxConstraints)
    {
        List<Size> naturalSizes
            = Children
                .ToList()
                .Select(GetChildSize).ToList();

        int numberOfInfiniteHeightChildren = naturalSizes.Count(size => float.IsPositiveInfinity(size.Height));
        float gapsTotalHeight = (naturalSizes.Count - 1) * Gap.Value;
        float totalHeightOfNotInfiniteHeightChildren
            = naturalSizes
                .Select(size => size.Height)
                .Aggregate((acc, current)
                    => float.IsPositiveInfinity(current) ? 0 : current);
        float remainingHeightForExpandedChildren
            = boxConstraints.MaxHeight - (totalHeightOfNotInfiniteHeightChildren + gapsTotalHeight);
        remainingHeightForExpandedChildren = Math.Max(remainingHeightForExpandedChildren, 0);

        float heightAcc = 0;
        for (int i = 0; i < Children.Count; i++)
        {
            Widget child = Children[i];
            float height = float.IsPositiveInfinity(naturalSizes[i].Height)
                ? remainingHeightForExpandedChildren / numberOfInfiniteHeightChildren
                : naturalSizes[i].Height;
            child.Size.Value = new(naturalSizes[i].Width, height );
            child.Offset.Value = new(0, heightAcc);
            heightAcc += height;
            if (i < Children.Count - 1)
                heightAcc += Gap.Value;
        }

        Size minContentSize = new(naturalSizes.Select(s => s.Width).Min(), heightAcc);
        Size desiredSize = minContentSize with { Height = ExpandVertically.Value ? boxConstraints.MaxHeight : heightAcc }; 
        
        return new LayoutResult(desiredSize, minContentSize);
        
        Size GetChildSize(Widget child)
        {
            ((float width, _), (_, _)) = child.PerformLayoutAndClamp(boxConstraints.LoosenWidth());
            ((_, float height), (_, _)) = child.PerformLayoutAndClamp(boxConstraints with
            {
                HeightConstraints = new(0, float.PositiveInfinity)
            });
            return new(width, height);
        }
        
        
    }
}