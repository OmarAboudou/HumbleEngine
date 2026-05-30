namespace HumbleEngine;

public abstract record HumbleRecord : IDisposable
{
    private readonly List<IDisposable> _disposables = [];


    public virtual void Dispose()
    {
        foreach (IDisposable disposable in _disposables) disposable.Dispose();
    }

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

    protected IPropertyListener<T> CreateProtectedProperty<T>(T initialValue, out Property<T> publicProperty)
    {
        Property<T> property = CreatePublicProperty(initialValue);
        publicProperty = property;
        return property.Listener;
    }

    protected ListProperty<T> CreatePublicListProperty<T>(IReadOnlyList<T>? initialElements = null)
    {
        ListProperty<T> listProperty = new(initialElements);
        _disposables.Add(listProperty);
        return listProperty;
    }

    protected IListPropertyListener<T> CreateProtectedListProperty<T>(IReadOnlyList<T> initialElements,
        out ListProperty<T> publicListProperty)
    {
        ListProperty<T> listProperty = CreatePublicListProperty(initialElements);
        _disposables.Add(listProperty);
        publicListProperty = listProperty;
        return listProperty.Listener;
    }
}