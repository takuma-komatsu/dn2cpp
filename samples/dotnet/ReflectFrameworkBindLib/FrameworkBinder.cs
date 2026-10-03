using System;
using System.ComponentModel;
using System.Reflection;

// dn2cpp reads this assembly as framework code, by its System.* name: its
// CreateDelegate calls are the framework's own, and its override of a framework
// generic virtual method is a framework body.
namespace ReflectFrameworkBindLib;

public static class FrameworkBinder
{
    public static string Call(MethodInfo method, object target) =>
        ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), target, method))();

    public static void Run(MethodInfo method, object target) =>
        ((Action)Delegate.CreateDelegate(typeof(Action), target, method))();
}

public class LibraryProvider : TypeDescriptionProvider
{
    public string Registered = "unregistered";

    public override void RegisterType<T>() => Registered = "library:" + typeof(T).Name;
}

public class LateProvider : TypeDescriptionProvider
{
    public string Registered = "unregistered";

    public override void RegisterType<T>() => Registered = "late:" + typeof(T).Name;
}
