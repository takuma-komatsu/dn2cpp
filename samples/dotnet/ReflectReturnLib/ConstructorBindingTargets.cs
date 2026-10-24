using System.Collections.Generic;

namespace ReflectReturnLib;

public sealed class ConstructorStringOnly
{
    public ConstructorStringOnly(string value) { }
}

public sealed class ConstructorPrivateOnly
{
    private ConstructorPrivateOnly(string value) { }
}

public sealed class ConstructorFifteen
{
    public ConstructorFifteen(int a, int b, int c, int d, int e, int f, int g,
        int h, int i, int j, int k, int l, int m, int n, int o) { }
}

public sealed class ConstructorDominant
{
    public readonly string Value;
    public ConstructorDominant(string? value) => Value = "string";
    public ConstructorDominant(object? value) => Value = "object";
}

public sealed class ConstructorOmittedDominant
{
    public readonly string Value;
    public ConstructorOmittedDominant(object? value) => Value = "object";
    public ConstructorOmittedDominant(string? value) => Value = "string";
}

public sealed class ConstructorAmbiguous
{
    public ConstructorAmbiguous(string? value) { }
    public ConstructorAmbiguous(int value) { }
}

public sealed class ConstructorWidened
{
    public readonly long Value;
    public ConstructorWidened(long value) => Value = value;
    public ConstructorWidened(string value) { }
}

public sealed class ConstructorNested<T>
{
    public ConstructorNested(ConstructorNested<List<T>> value) { }
}

public sealed class ConstructorPrivateDefault
{
    private ConstructorPrivateDefault() { }
}

public sealed class ConstructorPrivateGenericDefault<T>
{
    private ConstructorPrivateGenericDefault() { }
}

public sealed class ConstructorFieldOwner
{
    public ConstructorFieldTarget? Value;
}

public sealed class ConstructorFieldTarget
{
    public ConstructorFieldTarget(string value) { }
}
