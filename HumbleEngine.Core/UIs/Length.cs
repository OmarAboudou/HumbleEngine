namespace HumbleEngine.Core;

public abstract record Length()
{
    public float ComputeAndClamp(LengthConstraints constraints)
        => Math.Clamp(Compute(constraints), constraints.Min, constraints.Max);
    
    public abstract float Compute(LengthConstraints constraints); 
    
    public static implicit operator Length(float value)
        => new Pixel(value);
}