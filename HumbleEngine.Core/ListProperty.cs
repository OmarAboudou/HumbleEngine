using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HumbleEngine.Core;

public interface IListPropertyListener<T> : IReadOnlyList<T>, IDisposable
{
    public delegate void ElementAddedHandler(int index, T element);
    public void ConnectAddedElement(ElementAddedHandler callback);
    public void DisconnectAddedElement(ElementAddedHandler callback);
    
    public delegate void ElementRemovedHandler(int index, T element);
    public void ConnectRemovedElement(ElementRemovedHandler callback);
    public void DisconnectRemovedElement(ElementRemovedHandler callback);

    public void BindFrom<TOther>(IListPropertyListener<TOther> other, Func<TOther, T> transformationFrom);
    public void UnbindFrom<TOther>(IListPropertyListener<TOther> other, Func<TOther, T> transformationFrom);
    
    public void BindTo<TOther>(IListPropertyListener<TOther> other, Func<T, TOther> transformationTo)
        => other.BindFrom(this, transformationTo);
    public void UnbindTo<TOther>(IListPropertyListener<TOther> other, Func<T, TOther> transformationTo)
        => other.UnbindFrom(this, transformationTo);

    public void BindFrom(IListPropertyListener<T> other)
        => BindFrom(other, IdentityFunction);
    public void BindTo(IListPropertyListener<T> other)
        => BindTo(other, IdentityFunction);
    
    public void UnbindFrom(IListPropertyListener<T> other)
        => UnbindFrom(other, IdentityFunction);
    public void UnbindTo(IListPropertyListener<T> other)
        => UnbindTo(other, IdentityFunction);
    
    public void Bind2WayFrom<TOther>(
        IListPropertyListener<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        this.BindFrom(other, transformationFrom);
        other.BindFrom(this, transformationTo);
    }
    public void Bind2WayTo<TOther>(
        IListPropertyListener<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
        => other.Bind2WayFrom(this, transformationTo, transformationFrom);

    public void Unbind2WayFrom<TOther>(
        IListPropertyListener<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        other.UnbindFrom(this, transformationTo);
        this.UnbindFrom(other, transformationFrom);
    }
    public void Unbind2WayTo<TOther>(
        IListPropertyListener<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
        => other.Unbind2WayFrom(this, transformationTo, transformationFrom);
    
    public void Bind2WayFrom(IListPropertyListener<T> other)
        => Bind2WayFrom(other, IdentityFunction, IdentityFunction);
    public void Unbind2WayFrom(IListPropertyListener<T> other)
        => Unbind2WayFrom(other, IdentityFunction, IdentityFunction);
}

public sealed class ListProperty<T> : IListPropertyListener<T>, IList<T>
{

    internal ListProperty() : this(null) { }
    internal ListProperty(IReadOnlyList<T>? initialValues)
    {
        if (initialValues is not null)
        {
            _list.AddRange(initialValues);
        }
    }
    
    private readonly List<T> _list = [];
    public IListPropertyListener<T> Listener 
        => field ??= new ListPropertyListener<T>(this);
    
    private delegate bool RefCheckingMethod(out object? target);
    
    private readonly List<WeakReference<IListPropertyListener<T>.ElementAddedHandler>> _othersListeningToMeAdding = [];
    private readonly List<(object weakRefToOther, RefCheckingMethod refCheckingMethod, Delegate lambda)> _meListeningToOthersAdding = [];


    public void ConnectAddedElement(IListPropertyListener<T>.ElementAddedHandler callback)
    {
        CleanConnections();
        if (_othersListeningToMeAdding.Any(
                wr =>  wr.TryGetTarget(out var target) && target == callback)
           )
        {
            Console.WriteLine($"Callback {callback} is already connected to adding elements on {this} and cannot be connected multiple times.");
            return;
        }
        
        _othersListeningToMeAdding.Add(new WeakReference<IListPropertyListener<T>.ElementAddedHandler>(callback));
    }

    public void DisconnectAddedElement(IListPropertyListener<T>.ElementAddedHandler callback)
    {
        CleanConnections();

        _othersListeningToMeAdding.RemoveAll(wr =>
        {
            wr.TryGetTarget(out var target);
            return target == callback || target == null;
        });
    }

    private readonly List<WeakReference<IListPropertyListener<T>.ElementRemovedHandler>> _othersListeningToMeRemoving = [];
    private readonly List<(object weakRefToOther, RefCheckingMethod refCheckingMethod, Delegate lambda)> _meListeningToOthersRemoving = [];
    public void ConnectRemovedElement(IListPropertyListener<T>.ElementRemovedHandler callback)
    {
        CleanConnections();
        if (_othersListeningToMeRemoving.Any(
                wr =>  wr.TryGetTarget(out var target) && target == callback)
           )
        {
            Console.WriteLine($"Callback {callback} is already connected to removing elements on {this} and cannot be connected multiple times.");
            return;
        }
        
        _othersListeningToMeRemoving.Add(new WeakReference<IListPropertyListener<T>.ElementRemovedHandler>(callback));

    }
    public void DisconnectRemovedElement(IListPropertyListener<T>.ElementRemovedHandler callback)
    {
        CleanConnections();

        _othersListeningToMeRemoving.RemoveAll(wr =>
        {
            wr.TryGetTarget(out var target);
            return target == callback || target == null;
        });
    }

    private void EmitElementAdded(int index, T element)
    {
        CleanConnections();
        
        _othersListeningToMeAdding.ForEach(wr =>
        {
            if (wr.TryGetTarget(out var target))
                target(index, element);
        });
    }

    private void EmitElementRemoved(int index, T element)
    {
        CleanConnections();
        
        _othersListeningToMeRemoving.ForEach(wr =>
        {
            if (wr.TryGetTarget(out var target))
                target(index, element);
        });
    }

    public void BindFrom<TOther>(IListPropertyListener<TOther> other, Func<TOther, T> transformationFrom)
    {
        IReadOnlyList<T> elements = other.Select(transformationFrom).ToList();
        Clear();
        foreach (T element in elements)
        {
            Add(element);
        }

        other.ConnectAddedElement(OnAdding);
        other.ConnectRemovedElement(OnRemoving);
        
        WeakReference<IListPropertyListener<TOther>> weakReferenceToOther = new(other);
        RefCheckingMethod refCheckingMethod = Check;
        
        _meListeningToOthersAdding.Add( ( weakReferenceToOther, refCheckingMethod, OnAdding ) );
        _meListeningToOthersRemoving.Add( ( weakReferenceToOther, refCheckingMethod, OnRemoving ) );
        return;

        void OnAdding(int index, TOther element) 
            => Insert(index, transformationFrom(element));

        void OnRemoving(int index, TOther _)
            => RemoveAt(index);

        bool Check([NotNullWhen(true)] out object? target)
        {
            bool result = weakReferenceToOther.TryGetTarget(out IListPropertyListener<TOther>? t);
            target = t;
            return result;
        }
    }

    public void UnbindFrom<TOther>(IListPropertyListener<TOther> other, Func<TOther, T> transformationFrom)
    {
        _meListeningToOthersAdding.RemoveAll(tuple =>
        {
            (_, RefCheckingMethod refCheckingMethod, Delegate lambda) = tuple;
            refCheckingMethod(out var target);
            return ReferenceEquals(target, lambda) || target == null;
        });
        _meListeningToOthersRemoving.RemoveAll(tuple =>
        {
            (_, RefCheckingMethod refCheckingMethod, Delegate lambda) = tuple;
            refCheckingMethod(out var target);
            return ReferenceEquals(target, lambda) || target == null;
        });
    }

    private void CleanConnections()
    {
        _othersListeningToMeAdding.RemoveAll(wr => !wr.TryGetTarget(out _));
        _othersListeningToMeRemoving.RemoveAll(wr => !wr.TryGetTarget(out _));
    }


    public IEnumerator<T> GetEnumerator()
        => _list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() 
        => GetEnumerator();

    public void Add(T item)
        => this.Insert(_list.Count, item);

    public void Clear()
    {
        for (int i = _list.Count - 1; i >= 0; i--) 
            RemoveAt(i);
    }

    public bool Contains(T item)
        => _list.Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
        => _list.CopyTo(array, arrayIndex);

    public bool Remove(T item)
    {
        int index = _list.IndexOf(item);
        
        if (index == -1) return false;
        
        this.RemoveAt(index);
        return true;
    }

    public int Count => _list.Count;
    public bool IsReadOnly => false;
    public int IndexOf(T item)
        => _list.IndexOf(item);

    public void Insert(int index, T item)
    {
        _list.Insert(index, item);
        EmitElementAdded(index, item);
    }

    public void RemoveAt(int index)
    {
        T item = this[index];
        _list.RemoveAt(index);
        EmitElementRemoved(index, item);
    }

    public T this[int index]
    {
        get => _list[index];
        set
        {
            T currentValue = _list[index];
            if (!EqualityComparer<T>.Default.Equals(currentValue, value))
            {
                this.RemoveAt(index);
                this.Insert(index, value);
            }
        }
    }

    public void Dispose()
    {
        _othersListeningToMeAdding.Clear();
        _othersListeningToMeRemoving.Clear();
        _meListeningToOthersAdding.Clear();
        _meListeningToOthersRemoving.Clear();
    }
}

public sealed class ListPropertyListener<T> : IListPropertyListener<T>
{
    internal ListPropertyListener(ListProperty<T> listProperty)
    {
        _listProperty = listProperty;
    }

    private readonly ListProperty<T> _listProperty;

    public void ConnectAddedElement(IListPropertyListener<T>.ElementAddedHandler callback) 
        => _listProperty.ConnectAddedElement(callback);

    public void DisconnectAddedElement(IListPropertyListener<T>.ElementAddedHandler callback) 
        => _listProperty.DisconnectAddedElement(callback);

    public void ConnectRemovedElement(IListPropertyListener<T>.ElementRemovedHandler callback) 
        => _listProperty.ConnectRemovedElement(callback);

    public void DisconnectRemovedElement(IListPropertyListener<T>.ElementRemovedHandler callback) 
        => _listProperty.DisconnectRemovedElement(callback);

    public void BindFrom<TOther>(IListPropertyListener<TOther> other, Func<TOther, T> transformationFrom) 
        => _listProperty.BindFrom(other, transformationFrom);

    public void UnbindFrom<TOther>(IListPropertyListener<TOther> other, Func<TOther, T> transformationFrom) 
        => _listProperty.UnbindFrom(other, transformationFrom);

    public IEnumerator<T> GetEnumerator() 
        => _listProperty.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() 
        => ((IEnumerable)_listProperty).GetEnumerator();

    public int Count => _listProperty.Count;

    public T this[int index] => _listProperty[index];

    public void Dispose()
    {
        
    }
}

public static class ListPropertyExtensions{
public static void BindTo<TOther, T>(
        this IListPropertyListener<T> @this,
        IListPropertyListener<TOther> other,
        Func<T, TOther> transformation)
        => other.BindFrom(@this, transformation);
    public static void UnbindTo<TOther, T>(
        this IListPropertyListener<T> @this,
        IListPropertyListener<TOther> other,
        Func<T, TOther> transformation)
        => other.UnbindFrom(@this, transformation);

    public static void BindFrom<T>(
        this IListPropertyListener<T> @this,IListPropertyListener<T> other) 
        => @this.BindFrom(other, IdentityFunction);

    public static void UnbindFrom<T>(
        this IListPropertyListener<T> @this, IListPropertyListener<T> other)
        => @this.UnbindFrom(other, IdentityFunction);

    public static void BindTo<T>(
        this IListPropertyListener<T> @this,IListPropertyListener<T> other)
        => @this.BindTo(other, IdentityFunction);
    public static void UnbindTo<T>(
        this IListPropertyListener<T> @this,IListPropertyListener<T> other)
        => @this.UnbindTo(other, IdentityFunction);

    public static void Bind2WayFrom<T>(
        this IListPropertyListener<T> @this,IListPropertyListener<T> other)
        => @this.Bind2WayFrom(other, IdentityFunction, IdentityFunction);
    public static void Unbind2WayFrom<T>(
        this IListPropertyListener<T> @this,IListPropertyListener<T> other)
        => @this.Unbind2WayFrom(other, IdentityFunction, IdentityFunction);

    public static void Bind2WayTo<T>(
        this IListPropertyListener<T> @this,IListPropertyListener<T> other)
        => other.Bind2WayFrom(@this);
    public static void Unbind2WayTo<T>(
        this IListPropertyListener<T> @this,IListPropertyListener<T> other)
        => other.Unbind2WayFrom(@this);
    
    public static void Bind2WayFrom<TOther, T>(
        this IListPropertyListener<T> @this,
        IListPropertyListener<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        @this.BindFrom(other, transformationFrom);
        other.BindFrom(@this, transformationTo);
    }
    public static void Unbind2WayFrom<TOther, T>(
        this IListPropertyListener<T> @this,
        IListPropertyListener<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        other.UnbindFrom(@this, transformationTo);
        @this.UnbindFrom(other, transformationFrom);
    }

    public static void Bind2WayTo<TOther, T>(
        this IListPropertyListener<T> @this,
        IListPropertyListener<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo) 
        => other.Bind2WayFrom(@this, transformationTo, transformationFrom);
    public static void Unbind2WayTo<TOther, T>(
        this IListPropertyListener<T> @this,
        IListPropertyListener<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
        => other.Unbind2WayFrom(@this, transformationTo, transformationFrom);
}