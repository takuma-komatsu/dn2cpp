using System;

namespace HotUpdateNoCtor;

public static class GenericImportSurface
{
    public static string TypeName<T>()
    {
        return typeof(T).FullName;
    }

    public static T Echo<T>(T value)
    {
        return value;
    }
}

public static class LegacyImportSurface
{
    public static int Name()
    {
        return 73;
    }

    public static int Pair<T>()
    {
        return 91;
    }
}
