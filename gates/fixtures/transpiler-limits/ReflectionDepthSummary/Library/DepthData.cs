namespace ReflectionDepthLibrary;

public sealed class AccessorData
{
    public int Value
    {
        get { Descend<int>(); return 0; }
    }
    private static void Descend<T>() => Descend<List<T>>();
}

public sealed class ConstructorData
{
    public ConstructorData() => Descend<int>();
    private static void Descend<T>() => Descend<List<T>>();
}

public sealed class ConstructedData
{
    public int Value
    {
        get { Seed<List<List<List<int>>>>(); return 0; }
    }
    private static void Seed<T>() { }
}

public sealed class DirectAccessorData
{
    public int Value
    {
        get { Seed<List<List<List<int>>>>(); return 0; }
        set { Descend<int>(); }
    }
    private static void Seed<T>() { }
    private static void Descend<T>() => Descend<List<T>>();
}

public sealed class FactoryData
{
    public FactoryData() => Seed<List<List<List<int>>>>();
    private static void Seed<T>() { }
}

public sealed class EmptyFactoryData
{
    public EmptyFactoryData() { }
}

public abstract class AbstractConstructionData
{
    public void Reset() => Descend<int>();
    private static void Descend<T>() => Descend<List<T>>();
}
