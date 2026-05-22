using System.Collections;

namespace HumbleEngine;

public class ReactiveCollection<T> : IReadOnlyList<T>
{
    private readonly List<T> _list = new();

    private readonly MutableSignal<(int Index, T Item)> _itemAdded   = new();
    private readonly MutableSignal<(int Index, T Item)> _itemRemoved = new();
    private readonly MutableSignal                      _cleared      = new();
    private readonly MutableSignal                      _changed      = new();

    public Signal<(int Index, T Item)> ItemAdded   => _itemAdded.Signal;
    public Signal<(int Index, T Item)> ItemRemoved => _itemRemoved.Signal;
    public Signal                      Cleared      => _cleared.Signal;
    public Signal                      Changed      => _changed.Signal;

    public ReactiveCollection()
    {
        ItemAdded.Connect(_ => _changed.Emit());
        ItemRemoved.Connect(_ => _changed.Emit());
        Cleared.Connect(() => _changed.Emit());
    }

    public void Add(T item)
    {
        _list.Add(item);
        _itemAdded.Emit((_list.Count - 1, item));
    }

    public void Insert(int index, T item)
    {
        _list.Insert(index, item);
        _itemAdded.Emit((index, item));
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
        _itemRemoved.Emit((index, item));
    }

    public void Clear()
    {
        _list.Clear();
        _cleared.Emit();
    }

    public T this[int index] => _list[index];
    public int Count         => _list.Count;

    public IEnumerator<T> GetEnumerator()   => _list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
