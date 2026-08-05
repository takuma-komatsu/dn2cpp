#nullable disable
// Practical reflection: the generic plural
// CustomAttributeExtensions.GetCustomAttributes<T>() used in a foreach. It returns
// IEnumerable<T>; its real body casts an Attribute[] to IEnumerable<T>, which a foreach
// lowers to an IEnumerator<T> loop. A raw array carries no interface-dispatch table, so
// dn2cpp boxes the typeof(T)-filtered attribute array into an SZArrayEnumerable<T> (the
// array->IEnumerable<T> wrapper) at the intercepted call, giving a real IEnumerable<T> the
// foreach can enumerate. Covers an AllowMultiple attribute (foreach over several), counting
// via foreach, and a property's attribute. Diffed exact vs real .NET.
using System;
using System.Reflection;

namespace ReflectAttrEnumerableSubset;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RoleAttribute : Attribute
{
    public string Name;
    public int Level;
    public RoleAttribute(string n, int l) { Name = n; Level = l; }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class LabelAttribute : Attribute
{
    public string Text;
    public LabelAttribute(string t) { Text = t; }
}

[Role("admin", 3)]
[Role("user", 1)]
[Role("guest", 0)]
public class Service
{
    [Label("ID")]
    public int Id { get; set; }
}

public static class Program
{
    internal static void Run()
    {
        Type t = typeof(Service);

        // foreach over the generic plural GetCustomAttributes<T>() (multiple matches)
        foreach (RoleAttribute r in t.GetCustomAttributes<RoleAttribute>())
            Console.WriteLine(r.Name + ":" + r.Level);

        // counting via a second foreach over the same generic enumerable
        int n = 0;
        foreach (var r in t.GetCustomAttributes<RoleAttribute>())
            n++;
        Console.WriteLine("count=" + n);

        // a property's attribute through the same generic enumerable path
        PropertyInfo p = t.GetProperty("Id");
        foreach (LabelAttribute l in p.GetCustomAttributes<LabelAttribute>())
            Console.WriteLine("label=" + l.Text);
    }
}
