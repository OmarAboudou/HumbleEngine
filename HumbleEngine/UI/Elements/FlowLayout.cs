namespace HumbleEngine;

public abstract record FlowLayout : CompositeRenderElement
{
    public float          Gap            { get; init; }
    public MainAlignment  MainAlignment  { get; init; }
    public CrossAlignment CrossAlignment { get; init; }
}

public static class FlowLayoutExtensions
{
    public static T Gap<T>(this T el, float gap)                   where T : FlowLayout => el with { Gap            = gap };
    public static T MainAlignment<T>(this T el, MainAlignment v)   where T : FlowLayout => el with { MainAlignment  = v   };
    public static T CrossAlignment<T>(this T el, CrossAlignment v) where T : FlowLayout => el with { CrossAlignment = v   };
}