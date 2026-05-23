using System.Collections;

namespace HumbleEngine;

public class ListProperty<T> : IReadOnlyListProperty<T>, IList<T>
{
    private readonly List<T> _list = [];
    
    private readonly Signal<(T item, int index)> _added = new();
    private readonly Signal<(T item, int index)> _removed = new();

    public ReadOnlySignal<(T item, int index)> Added 
        => _added.AsReadOnly();
    public ReadOnlySignal<(T item, int index)> Removed 
        => _removed.AsReadOnly();

    public IEnumerator<T> GetEnumerator() 
        => _list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() 
        => GetEnumerator();

    public void Add(T item)
    {
        Insert(_list.Count, item);
    }

    public void Clear()
    {
        for (int i = _list.Count - 1; i >= 0; i--)
        {
            RemoveAt(i);
        }
    }

    public bool Contains(T item)
        => _list.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) 
        => _list.CopyTo(array, arrayIndex);

    public bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index != -1)
        {
            RemoveAt(index);
            return true;
        }
        return false;
    }

    public int Count => _list.Count;
    public bool IsReadOnly => false;
    public int IndexOf(T item) => _list.IndexOf(item);
    public void Insert(int index, T item)
    {
        _list.Insert(index, item);
        _added.Emit((item, index));
    }

    public void RemoveAt(int index)
    {
        T item = this[index];
        _list.RemoveAt(index);
        _removed.Emit((item, index));
    }

    public T this[int index]
    {
        get => _list[index];
        set
        {
            T item = _list[index];
            if(EqualityComparer<T>.Default.Equals(item, value))
                return;
            
            RemoveAt(index);
            Insert(index, value);
        }
    }
}

public class ReadOnlyListProperty<T> : IReadOnlyListProperty<T>
{
    private ListProperty<T> _listProperty;

    internal ReadOnlyListProperty(ListProperty<T> listProperty)
        => _listProperty  = listProperty;

    public IEnumerator<T> GetEnumerator() 
        => _listProperty.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() 
        => ((IEnumerable)_listProperty).GetEnumerator();

    public int Count => _listProperty.Count;
    public T this[int index] => _listProperty[index];
}

public interface IReadOnlyListProperty<out T> : IReadOnlyList<T>
{
    
}