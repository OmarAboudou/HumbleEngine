namespace HumbleEngine.Core;

public class HumbleObject : IDisposable
{
    private readonly List<IDisposable> _disposables = [];

    protected Signal CreateSignal(out Action emitter)
    {
        Signal s = new(out emitter);
        _disposables.Add(s);
        return s;
    }

    protected Signal<T1> CreateSignal<T1>(string arg1Name, out Action<T1> emitter)
    {
        Signal<T1> s = new(arg1Name, out emitter);
        _disposables.Add(s);
        return s;
    }

    protected Signal<T1, T2> CreateSignal<T1, T2>(string arg1Name, string arg2Name, out Action<T1, T2> emitter)
    {
        Signal<T1, T2> s = new(arg1Name, arg2Name, out emitter);
        _disposables.Add(s);
        return s;
    }

    protected EditableProperty<T> CreateProperty<T>(T initialValue)
    {
        EditableProperty<T> property = new(initialValue);
        _disposables.Add(property);
        return property;
    }


    public virtual void Dispose()
    {
        foreach (IDisposable disposable in _disposables)
        {
            disposable.Dispose();
        }
    }
}