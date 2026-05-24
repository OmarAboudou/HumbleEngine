namespace HumbleEngine;

// Suit un ReadOnlyProperty<T> et interpole automatiquement vers chaque nouvelle valeur.
public sealed class ImplicitAnimation<T> : IAnimation<T>
{
    private readonly Func<T, T, float, T> _lerp;
    private readonly Func<float, float>   _easing;
    private readonly float                _duration;
    private          T                    _from;
    private          T                    _to;
    private          float                _elapsed;

    public T    Value      => _lerp(_from, _to, _easing(Math.Clamp(_elapsed / _duration, 0f, 1f)));
    public bool IsComplete => _elapsed >= _duration;

    public ImplicitAnimation(ReadOnlyProperty<T> target, float duration,
                              Func<T, T, float, T> lerp, Func<float, float>? easing = null)
    {
        _lerp     = lerp;
        _easing   = easing ?? Easing.Linear;
        _duration = MathF.Max(duration, float.Epsilon);
        _from     = target.Value;
        _to       = target.Value;
        _elapsed  = _duration;

        target.ValueChanged.Connect((_, next) =>
        {
            _from    = Value; // part de la position animée courante (interruption propre)
            _to      = next;
            _elapsed = 0f;
        });
    }

    public void Advance(double delta) => _elapsed += (float)delta;
}
