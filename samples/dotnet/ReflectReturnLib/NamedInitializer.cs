using System;

namespace ReflectReturnLib;

public static class NamedInitializerSignals
{
    public static int Cold;
    public static int Inherited;
    public static int AppInherited;
    public static int Helper;
    public static int Generic;
    public static string GenericName;
}

public abstract class NamedColdInitializer
{
    private NamedColdInitializer() { }
    static NamedColdInitializer() { NamedInitializerSignals.Cold++; }
}

public abstract class NamedInitializerBase
{
    protected NamedInitializerBase() { }
    static NamedInitializerBase() { NamedInitializerSignals.Inherited++; }
}

public abstract class NamedInitializerDerived : NamedInitializerBase
{
    private NamedInitializerDerived() { }
}

public abstract class NamedAppInitializerBase
{
    protected NamedAppInitializerBase() { }
    static NamedAppInitializerBase() { NamedInitializerSignals.AppInherited++; }
}

public abstract class NamedGenericInitializer<T>
{
    private NamedGenericInitializer() { }
    static NamedGenericInitializer()
    {
        NamedInitializerSignals.Generic++;
        NamedInitializerSignals.GenericName = typeof(T).Name;
    }
}

public abstract class NamedHelperInitializer
{
    private static int count;
    private NamedHelperInitializer() { }
    static NamedHelperInitializer() { Record(); }
    private static void Record()
    {
        count++;
        NamedInitializerSignals.Helper = count;
    }
}
