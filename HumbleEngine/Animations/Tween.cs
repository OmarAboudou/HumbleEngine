namespace HumbleEngine;

public sealed class Tween<T> : IAnimation<T>
{
    private readonly Func<T, T, float, T> _lerp;
    private          Func<float, float>   _easing;
    private          T                    _from;
    private          T                    _to;
    private          float                _duration;
    private          float                _elapsed;

    public T    Value      => _lerp(_from, _to, _easing(Math.Clamp(_elapsed / _duration, 0f, 1f)));
    public bool IsComplete => _elapsed >= _duration;

    public Tween(T initial, Func<T, T, float, T> lerp, float duration = 0.3f, Func<float, float>? easing = null)
    {
        _lerp     = lerp;
        _easing   = easing ?? Easing.Linear;
        _duration = MathF.Max(duration, float.Epsilon);
        _from     = initial;
        _to       = initial;
        _elapsed  = _duration; // complet dès la création : Value == initial
    }

    // Démarre une transition vers target. Idempotent si target == _to en cours.
    // L'interruption est propre : _from = Value (position courante).
    public void To(T target, float? duration = null, Func<float, float>? easing = null)
    {
        if (EqualityComparer<T>.Default.Equals(target, _to)) return;
        _from    = Value;
        _to      = target;
        _elapsed = 0f;
        if (duration.HasValue) _duration = MathF.Max(duration.Value, float.Epsilon);
        if (easing != null)    _easing   = easing;
    }

    public void Advance(double delta) => _elapsed += (float)delta;
}
