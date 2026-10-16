using System;

namespace AdmissionFilters;

// Naming the derived library owner must retain the selected private base body.
public class LibraryPrivateBindingBase
{
    private static string Pick() => "library private";
    private static string Pick(int value) => LibraryNamedBindingProbe.Touch();
    private static string Other() => LibraryNamedBindingProbe.Touch();
}
public sealed class LibraryPrivateBinding : LibraryPrivateBindingBase { }

public class LibraryPrivateTemplateBase<T>
{
    private static string Pick() => "library template:" + typeof(T).Name;
    private static string Pick(int value) => LibraryNamedBindingProbe.Touch();
    private static string Other() => LibraryNamedBindingProbe.Touch();
}
public sealed class LibraryPrivateTemplate<T> : LibraryPrivateTemplateBase<T> { }

public class LibraryPrivateOpaqueBase<T>
{
    private static string Pick() => "library opaque:" + typeof(T).Name;
    private static string Pick(int value) => LibraryNamedBindingProbe.Touch();
    private static string Other() => LibraryNamedBindingProbe.Touch();
}
public sealed class LibraryPrivateOpaque<T> : LibraryPrivateOpaqueBase<T> { }

public class LibraryPrivateStaticBase<T>
{
    private static string Pick() => LibraryNamedDependency<T>.Text();
    private static string Pick(int value) => LibraryNamedBindingProbe.Touch();
    private static string Other() => LibraryNamedBindingProbe.Touch();
}
public sealed class LibraryPrivateStatic<T> : LibraryPrivateStaticBase<T> { }

static class LibraryNamedDependency<T>
{
    public static string Text() => "library static:" + typeof(T).Name;
}

static class LibraryNamedBindingProbe
{
    static LibraryNamedBindingProbe() => Console.WriteLine("unselected library binding initialized");
    public static string Touch() => "unselected library binding";
}
