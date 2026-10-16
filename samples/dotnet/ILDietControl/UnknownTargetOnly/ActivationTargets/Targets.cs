namespace ActivationTargets;

// Library types whose constructors nothing calls, so the image omits their rows.
public sealed class LibraryClass
{
    private readonly string _label;

    public LibraryClass() => _label = "library class";

    public LibraryClass(int count) => _label = "library class " + count;

    public override string ToString() => _label;
}

// Boxed by the application, so only its explicit constructor is missing.
public struct LibraryStruct
{
    public int Value;

    public LibraryStruct() => Value = 42;

    public override string ToString() => "library struct " + Value;
}

public sealed class ArgumentsOnly
{
    public ArgumentsOnly(int count) => Count = count;

    public int Count { get; }
}
