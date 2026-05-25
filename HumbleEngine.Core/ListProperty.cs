using System.Collections;

namespace HumbleEngine.Core;

public interface IListProperty<T> : IReadOnlyList<T>
{
    public IReadOnlyList<T> List { get; }
    
    public delegate void ElementAddedHandler(int index, T element);
    public void ConnectAddedElement(ElementAddedHandler callback);
    public void DisconnectAddedElement(ElementAddedHandler callback);
    
    public delegate void ElementRemovedHandler(int index, T element);
    public void ConnectRemovedElement(ElementRemovedHandler callback);
    public void DisconnectRemovedElement(ElementRemovedHandler callback);

    public void BindFrom<TOther>(IListProperty<TOther> other, Func<TOther, T> transformationFrom);
    public void UnbindFrom<TOther>(IListProperty<TOther> other, Func<TOther, T> transformationFrom);
    
    public void BindTo<TOther>(IListProperty<TOther> other, Func<T, TOther> transformationTo)
        => other.BindFrom(this, transformationTo);
    public void UnbindTo<TOther>(IListProperty<TOther> other, Func<T, TOther> transformationTo)
        => other.UnbindFrom(this, transformationTo);

    public void BindFrom(IListProperty<T> other)
        => BindFrom(other, IdentityFunction);
    public void BindTo(IListProperty<T> other)
        => BindTo(other, IdentityFunction);
    
    public void UnbindFrom(IListProperty<T> other)
        => UnbindFrom(other, IdentityFunction);
    public void UnbindTo(IListProperty<T> other)
        => UnbindTo(other, IdentityFunction);
    
    public void Bind2WayFrom<TOther>(
        IListProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        this.BindFrom(other, transformationFrom);
        other.BindFrom(this, transformationTo);
    }
    public void Bind2WayTo<TOther>(
        IListProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
        => other.Bind2WayFrom(this, transformationTo, transformationFrom);

    public void Unbind2WayFrom<TOther>(
        IListProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        other.UnbindFrom(this, transformationTo);
        this.UnbindFrom(other, transformationFrom);
    }
    public void Unbind2WayTo<TOther>(
        IListProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
        => other.Unbind2WayFrom(this, transformationTo, transformationFrom);
    
    public void Bind2WayFrom(IListProperty<T> other)
        => Bind2WayFrom(other, IdentityFunction, IdentityFunction);
    public void Unbind2WayFrom(IListProperty<T> other)
        => Unbind2WayFrom(other, IdentityFunction, IdentityFunction);
}

public sealed class EditableListProperty<T> : IListProperty<T>, IList<T>
{
    public EditableListProperty()
    {
        List = _list.AsReadOnly();
    }
    
    private List<T> _list = [];
    public IReadOnlyList<T> List { get; } 
    
    private delegate bool RefCheckingMethod(out object target);
    
    private readonly List<WeakReference<IListProperty<T>.ElementAddedHandler>> _othersListeningToMeAdding = [];
    private readonly List<(object weakRefToOther, RefCheckingMethod refCheckingMethod, Delegate lambda)> _meListeningToOthersAdding = [];


    public void ConnectAddedElement(IListProperty<T>.ElementAddedHandler callback)
    {
        CleanConnections();
        if (_othersListeningToMeAdding.Any(
                wr =>  wr.TryGetTarget(out var target) && target == callback)
           )
        {
            Console.WriteLine($"Callback {callback} is already connected to adding elements on {this} and cannot be connected multiple times.");
            return;
        }
        
        _othersListeningToMeAdding.Add(new WeakReference<IListProperty<T>.ElementAddedHandler>(callback));
    }

    public void DisconnectAddedElement(IListProperty<T>.ElementAddedHandler callback)
    {
        CleanConnections();

        _othersListeningToMeAdding.RemoveAll(wr =>
        {
            wr.TryGetTarget(out var target);
            return target == callback || target == null;
        });
    }

    private readonly List<WeakReference<IListProperty<T>.ElementRemovedHandler>> _othersListeningToMeRemoving = [];
    private readonly List<(object weakRefToOther, RefCheckingMethod refCheckingMethod, Delegate lambda)> _meListeningToOthersRemoving = [];
    public void ConnectRemovedElement(IListProperty<T>.ElementRemovedHandler callback)
    {
        CleanConnections();
        if (_othersListeningToMeRemoving.Any(
                wr =>  wr.TryGetTarget(out var target) && target == callback)
           )
        {
            Console.WriteLine($"Callback {callback} is already connected to removing elements on {this} and cannot be connected multiple times.");
            return;
        }
        
        _othersListeningToMeRemoving.Add(new WeakReference<IListProperty<T>.ElementRemovedHandler>(callback));

    }
    public void DisconnectRemovedElement(IListProperty<T>.ElementRemovedHandler callback)
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

    public void BindFrom<TOther>(IListProperty<TOther> other, Func<TOther, T> transformationFrom)
    {
        IReadOnlyList<T> elements = other.List.Select(transformationFrom).ToList();
        Clear();
        foreach (T element in elements)
        {
            Add(element);
        }

        other.ConnectAddedElement(OnAdding);
        other.ConnectRemovedElement(OnRemoving);
        
        WeakReference<IListProperty<TOther>> weakReferenceToOther = new(other);
        RefCheckingMethod refCheckingMethod = Check;

        ;
        _meListeningToOthersAdding.Add( ( weakReferenceToOther, refCheckingMethod, OnAdding ) );
        _meListeningToOthersRemoving.Add( ( weakReferenceToOther, refCheckingMethod, OnRemoving ) );
        return;

        void OnAdding(int index, TOther element) 
            => Insert(index, transformationFrom(element));

        void OnRemoving(int index, TOther _)
            => RemoveAt(index);

        bool Check(out object target)
        {
            bool result = weakReferenceToOther.TryGetTarget(out IListProperty<TOther> t);
            target = t;
            return result;
        }
    }

    public void UnbindFrom<TOther>(IListProperty<TOther> other, Func<TOther, T> transformationFrom)
    {
        _meListeningToOthersAdding.RemoveAll(tuple =>
        {
            (_, RefCheckingMethod refCheckingMethod, Delegate lambda) = tuple;
            refCheckingMethod(out var target);
            return target == lambda || target == null;
        });
        _meListeningToOthersRemoving.RemoveAll(tuple =>
        {
            (_, RefCheckingMethod refCheckingMethod, Delegate lambda) = tuple;
            refCheckingMethod(out var target);
            return target == lambda || target == null;
        });
    }

    private void CleanConnections()
    {
        _othersListeningToMeAdding.RemoveAll(wr => !wr.TryGetTarget(out _));
        _othersListeningToMeRemoving.RemoveAll(wr => !wr.TryGetTarget(out _));
    }

    private EditableListProperty<T> ExtractEditableListProperty<T>(IListProperty<T> source)
    {
        if (source is EditableListProperty<T> editableListProperty)
        {
            return editableListProperty;
        }
        if (source is ListProperty<T> nonEditableListProperty)
        {
            return nonEditableListProperty._editableListProperty;
        }

        throw new Exception($"Could not extract an {nameof(EditableListProperty<T>)} from {source}");
    }


    public IEnumerator<T> GetEnumerator()
        => List.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() 
        => GetEnumerator();

    public void Add(T item)
        => this.Insert(List.Count, item);

    public void Clear()
    {
        for (int i = List.Count - 1; i >= 0; i--) 
            RemoveAt(i);
    }

    public bool Contains(T item)
        => List.Contains(item);

    public void CopyTo(T[] array, int arrayIndex)
        => _list.CopyTo(array, arrayIndex);

    public bool Remove(T item)
    {
        int index = _list.IndexOf(item);
        
        if (index == -1) return false;
        
        this.RemoveAt(index);
        return true;
    }

    public int Count => List.Count;
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
        get => List[index];
        set
        {
            T currentValue = List[index];
            if (!EqualityComparer<T>.Default.Equals(currentValue, value))
            {
                this.RemoveAt(index);
                this.Insert(index, value);
            }
        }
    }
}

public sealed class ListProperty<T> : IListProperty<T>
{
    internal ListProperty(EditableListProperty<T> editableListProperty)
    {
        _editableListProperty = editableListProperty;
    }
    
    internal EditableListProperty<T> _editableListProperty;
    public IReadOnlyList<T> List => _editableListProperty.List;

    public void ConnectAddedElement(IListProperty<T>.ElementAddedHandler callback) 
        => _editableListProperty.ConnectAddedElement(callback);

    public void DisconnectAddedElement(IListProperty<T>.ElementAddedHandler callback) 
        => _editableListProperty.DisconnectAddedElement(callback);

    public void ConnectRemovedElement(IListProperty<T>.ElementRemovedHandler callback) 
        => _editableListProperty.ConnectRemovedElement(callback);

    public void DisconnectRemovedElement(IListProperty<T>.ElementRemovedHandler callback) 
        => _editableListProperty.DisconnectRemovedElement(callback);

    public void BindFrom<TOther>(IListProperty<TOther> other, Func<TOther, T> transformationFrom) 
        => _editableListProperty.BindFrom(other, transformationFrom);

    public void UnbindFrom<TOther>(IListProperty<TOther> other, Func<TOther, T> transformationFrom) 
        => _editableListProperty.UnbindFrom(other, transformationFrom);

    public IEnumerator<T> GetEnumerator() 
        => _editableListProperty.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() 
        => ((IEnumerable)_editableListProperty).GetEnumerator();

    public int Count => _editableListProperty.Count;

    public T this[int index] => _editableListProperty[index];
}

public static class ListPropertyExtensions{
public static void BindTo<TOther, T>(
        this IListProperty<T> This,
        IListProperty<TOther> other,
        Func<T, TOther> transformation)
        => other.BindFrom(This, transformation);
    public static void UnbindTo<TOther, T>(
        this IListProperty<T> This,
        IListProperty<TOther> other,
        Func<T, TOther> transformation)
        => other.UnbindFrom(This, transformation);

    public static void BindFrom<T>(
        this IListProperty<T> This,IListProperty<T> other) 
        => This.BindFrom(other, IdentityFunction);

    public static void UnbindFrom<T>(
        this IListProperty<T> This, IListProperty<T> other)
        => This.UnbindFrom(other, IdentityFunction);

    public static void BindTo<T>(
        this IListProperty<T> This,IListProperty<T> other)
        => This.BindTo(other, IdentityFunction);
    public static void UnbindTo<T>(
        this IListProperty<T> This,IListProperty<T> other)
        => This.UnbindTo(other, IdentityFunction);

    public static void Bind2WayFrom<T>(
        this IListProperty<T> This,IListProperty<T> other)
        => This.Bind2WayFrom(other, IdentityFunction, IdentityFunction);
    public static void Unbind2WayFrom<T>(
        this IListProperty<T> This,IListProperty<T> other)
        => This.Unbind2WayFrom(other, IdentityFunction, IdentityFunction);

    public static void Bind2WayTo<T>(
        this IListProperty<T> This,IListProperty<T> other)
        => other.Bind2WayFrom(This);
    public static void Unbind2WayTo<T>(
        this IListProperty<T> This,IListProperty<T> other)
        => other.Unbind2WayFrom(This);
    
    public static void Bind2WayFrom<TOther, T>(
        this IListProperty<T> This,
        IListProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        This.BindFrom(other, transformationFrom);
        other.BindFrom(This, transformationTo);
    }
    public static void Unbind2WayFrom<TOther, T>(
        this IListProperty<T> This,
        IListProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
    {
        other.UnbindFrom(This, transformationTo);
        This.UnbindFrom(other, transformationFrom);
    }

    public static void Bind2WayTo<TOther, T>(
        this IListProperty<T> This,
        IListProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo) 
        => other.Bind2WayFrom(This, transformationTo, transformationFrom);
    public static void Unbind2WayTo<TOther, T>(
        this IListProperty<T> This,
        IListProperty<TOther> other,
        Func<TOther, T> transformationFrom,
        Func<T, TOther> transformationTo)
        => other.Unbind2WayFrom(This, transformationTo, transformationFrom);
}