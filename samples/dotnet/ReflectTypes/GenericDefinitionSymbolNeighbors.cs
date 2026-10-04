namespace dn2cpp_nested_neighbors;

internal sealed class Closed<T>
{
    public string ArrayName() => new T[0].GetType().Name;
}

internal sealed class Open<T> { }
