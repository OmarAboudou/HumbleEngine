using System.Collections;

namespace HumbleEngine;

public class ReactiveCollection<T> : IReactiveCollection<T>
{
    private readonly ReactiveProperty<List<T>> _innerList = new([]);
    public void Connect(Action<IReadOnlyList<T>> listener) => _innerList.Connect(listener);
    public void Disconnect(Action<IReadOnlyList<T>> listener) => _innerList.Disconnect(listener);

    public ISignal<IReadOnlyList<T>, IReadOnlyList<T>> Reaffected => _innerList.Reaffected;

    public IReadOnlyList<T> Value
    {
        get => _innerList.Value;
        set => _innerList.Value = value as List<T> ?? value.ToList();
    }

    private readonly MutableSignal<int, T> _itemAdded   = new();
    private readonly MutableSignal<int, T> _itemRemoved = new();
    private readonly MutableSignal                      _cleared      = new();
    private readonly MutableSignal                      _changed      = new();

    public ISignal<int, T> ItemAdded   => _itemAdded.Signal;
    public ISignal<int, T> ItemRemoved => _itemRemoved.Signal;
    public ISignal                      Cleared      => _cleared.Signal;
    public ISignal                      Changed      => _changed.Signal;

    public ReactiveCollection()
    {
        ItemAdded.Connect( EmitChanged );
        ItemRemoved.Connect( EmitChanged );
        Cleared.Connect( EmitChanged );
        Reaffected.Connect((oldList, newList) =>
        {
            ItemAdded.Disconnect( EmitChanged );
            ItemRemoved.Disconnect( EmitChanged );
            
            if (oldList != null)
            {
                for (int i = 0; i < oldList.Count; i++)
                {
                    _itemRemoved.Emit(i, oldList[i]);
                }
            }

            if (newList != null)
            {
                for (int i = 0; i < newList.Count; i++)
                {
                    _itemAdded.Emit(i, newList[i]);
                }
            }
            
            ItemAdded.Connect( EmitChanged );
            ItemRemoved.Connect( EmitChanged );
            EmitChanged();
        });
    }

    public void Add(T item)
    {
        _innerList.Value.Add(item);
        _itemAdded.Emit(_innerList.Value.Count - 1, item);
    }

    public int IndexOf(T item)
    {
        return _innerList.Value.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        _innerList.Value.Insert(index, item);
        _itemAdded.Emit(index, item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _innerList.Value.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        int index = _innerList.Value.IndexOf(item);
        if (index < 0) return false;
        RemoveAt(index);
        return true;
    }

    public void RemoveAt(int index)
    {
        var item = _innerList.Value[index];
        _innerList.Value.RemoveAt(index);
        _itemRemoved.Emit(index, item);
    }

    public void Clear()
    {
        _innerList.Value.Clear();
        _cleared.Emit();
    }

    public bool Contains(T item)
    {
        return _innerList.Value.Contains(item);
    }

    public T this[int index]
    {
        get => _innerList.Value[index];
        set
        {
            RemoveAt(index);
            Insert(index, value);
        }
    }

    public int Count         => _innerList.Value.Count;
    public bool IsReadOnly => (_innerList.Value as IList<T>).IsReadOnly;

    public IEnumerator<T> GetEnumerator()   => _innerList.Value.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void EmitChanged() => _changed.Emit();
    private void EmitChanged<T1, T2>(T1 a, T2 b) => EmitChanged();
    
}
