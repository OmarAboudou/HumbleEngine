namespace HumbleEngine.Core;

public abstract class HumbleObject : IDisposable
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

    protected Property<T> CreatePublicProperty<T>(T initialValue)
    {
        Property<T> property = new(initialValue);
        _disposables.Add(property);
        return property;
    }

    protected IPropertyListener<T> CreateProtectedProperty<T>(T initialValue, out Action<T> setter)
    {
        Property<T> property = CreatePublicProperty(initialValue);
        setter = (v) => property.Value = v;
        return property.Listener;
    }

    protected EditableListProperty<T> CreateListProperty<T>(IReadOnlyList<T>? initialElements = null)
    {
        EditableListProperty<T> listProperty = new(initialElements);
        _disposables.Add(listProperty);
        return listProperty;
    }


    public virtual void Dispose()
    {
        foreach (IDisposable disposable in _disposables)
        {
            disposable.Dispose();
        }
    }
}