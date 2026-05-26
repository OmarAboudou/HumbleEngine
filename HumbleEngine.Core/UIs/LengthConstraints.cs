namespace HumbleEngine.Core;

public readonly record struct LengthConstraints(float MinLength, float MaxLength)
{
    public LengthConstraints Loosen()
        => this with{ MinLength = 0 };
    
}
