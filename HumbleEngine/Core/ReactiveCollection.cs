using System.Collections;

namespace HumbleEngine;

public class ReactiveCollection<T> : IReadOnlyList<T>
{
    private readonly List<T> _list = new();

    public Signal<(int Index, T Item)> ItemAdded   { get; }
    public Signal<(int Index, T Item)> ItemRemoved { get; }
    public Signal                      Reset        { get; }

    private readonly Action<(int Index, T Item)> _emitItemAdded;
    private readonly Action<(int Index, T Item)> _emitItemRemoved;
    private readonly Action                      _emitReset;

    public ReactiveCollection()
    {
        (ItemAdded,   _emitItemAdded)   = Signal<(int, T)>.Create();
        (ItemRemoved, _emitItemRemoved) = Signal<(int, T)>.Create();
        (Reset,       _emitReset)       = Signal.Create();
    }

    public void Add(T item)
    {
        _list.Add(item);
        _emitItemAdded((_list.Count - 1, item));
    }

    public void Insert(int index, T item)
    {
        _list.Insert(index, item);
        _emitItemAdded((index, item));
    }

    public bool Remove(T item)
    {
        int index = _list.IndexOf(item);
        if (index < 0) return false;
        RemoveAt(index);
        return true;
    }

    public void RemoveAt(int index)
    {
        var item = _list[index];
        _list.RemoveAt(index);
        _emitItemRemoved((index, item));
    }

    public void Clear()
    {
        _list.Clear();
        _emitReset();
    }

    public T this[int index] => _list[index];
    public int Count         => _list.Count;

    public IEnumerator<T> GetEnumerator()          => _list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator()        => GetEnumerator();
}
