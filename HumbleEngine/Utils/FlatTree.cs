namespace HumbleEngine.Utils;

public class IncoherentFlatTreeException<T>(FlatTree<T> flatTree) : Exception($"FlatTree : {flatTree} is incoherent.")
    where T : class;

public class FlatTree<T>
    where T : class
{
    public FlatTree(T root)
    {
        _root = root;
        _items = [root];
        _subtreeSizes = [1];
        _itemToIndex = new(){ { root, 0 } };
    }
    
    private readonly T _root;
    private readonly List<T> _items;
    private readonly List<int> _subtreeSizes;
    private readonly Dictionary<T, int> _itemToIndex;
    
    public int GetIndex(T item) => _itemToIndex[item];
    

    public IReadOnlyList<T> GetChildren(T item) => GetChildren(GetIndex(item));
    public IReadOnlyList<T> GetChildren(int parentIndex)
    {
        CheckBounds(parentIndex);
        
        List<T> children = new();
        int end = parentIndex + _subtreeSizes[parentIndex];

        for (int i = parentIndex + 1; i < end; i += _subtreeSizes[i])
            children.Add(_items[i]);

        return children;
    }
    

    public T? GetParent(T item) => GetParent(GetIndex(item));
    public T? GetParent(int index)
    {
        CheckBounds(index);

        if (index == 0)
            return null;

        for (int p = index - 1; p >= 0; p--)
            if (p + _subtreeSizes[p] > index)
                return _items[p];

        return null;
    }
    
    
    private void CheckBounds(int index)
    {
        if(index < 0 || index >= _items.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
    }
}