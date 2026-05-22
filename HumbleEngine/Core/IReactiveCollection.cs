namespace HumbleEngine;

public interface IReactiveCollection<T> : IReadOnlyReactiveCollection<T>, IReactiveProperty<IReadOnlyList<T>>, IList<T>
{
    
}