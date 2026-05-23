using HumbleEngine;
using SilkMot = Silk.NET.Input.IMotor;

namespace HumbleEngine.Silk;

public class SilkMotor : IMotor
{
    private readonly SilkMot _motor;

    public SilkMotor(SilkMot motor) => _motor = motor;

    public int   Index { get => _motor.Index; }
    public float Speed { get => _motor.Speed; set => _motor.Speed = value; }
}
