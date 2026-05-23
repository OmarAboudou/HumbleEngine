namespace HumbleEngine;

public readonly struct Deadzone
{
    public float         Value  { get; }
    public DeadzoneMethod Method { get; }

    public Deadzone(float value, DeadzoneMethod method)
    {
        Value  = value;
        Method = method;
    }

    public float Apply(float value) => Method switch
    {
        DeadzoneMethod.Traditional      => MathF.Abs(value) < Value ? 0f : value,
        DeadzoneMethod.AdaptiveGradient => MathF.Abs(value) < Value ? 0f
            : MathF.Sign(value) * (MathF.Abs(value) - Value) / (1f - Value),
        _ => value
    };
}
