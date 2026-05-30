namespace HumbleEngine;

public abstract record PrimitiveWidget : Widget
{
    protected virtual void OnMount(){}
    protected virtual void OnUnmount(){}
    
    public abstract Size ComputeAndSetDesiredSize(BoxConstraints constraints);
}