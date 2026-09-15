using System;

namespace MetadataAssemblyCollision;

public sealed class Subject<T> { }

public static class MetadataDefinition
{
    public static Type Read() => typeof(Subject<>);
}
