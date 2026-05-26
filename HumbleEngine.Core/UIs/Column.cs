namespace HumbleEngine.Core;

public record Column : LinearLayout<Widget>
{
    protected override LayoutResult PerformLayout(BoxConstraints boxConstraints)
    {
        List<Size> naturalSizes
            = Children
                .ToList()
                .ForEach(Child => {
                    
                }).ToList();

        Size GetChildSize(Widget child)
        {
            float width = child.PerformLayout(new())
        }
        
    }
}