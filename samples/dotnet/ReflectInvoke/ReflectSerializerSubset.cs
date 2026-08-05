#nullable enable
// SUBJECT: an attribute-driven mini-serializer combining the reflection features
// the way serialization and DI code does — resolve a type by name, instantiate
// via Activator, populate and read through PropertyInfo/FieldInfo, rename from an
// attribute, invoke by name, walk an enum through a runtime Type. Each piece has
// its own section elsewhere; this one asserts they COMPOSE.
using System;
using System.Reflection;
using System.Text;
using System.Collections.Generic;

namespace ReflectSerializerSubset;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ColumnAttribute : Attribute
{
    public string Name { get; }
    public int Order { get; set; }
    public ColumnAttribute(string name) { Name = name; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class IgnoreAttribute : Attribute { }

public enum Status { Inactive = 0, Active = 1, Banned = 2 }

public class Account
{
    [Column("user_name", Order = 1)]
    public string Name { get; set; } = "";

    [Column("age_years", Order = 2)]
    public int Age { get; set; }

    public Status Status { get; set; }

    [Ignore]
    public string Secret { get; set; } = "hidden";

    [Column("note")]
    public string Note = "";

    public string Summary() => $"{Name}/{Age}/{Status}";
}

public static class MiniSerializer
{
    // "key=value" per public property and field, keyed by [Column] and skipping
    // anything marked [Ignore].
    public static string Serialize(object o)
    {
        Type t = o.GetType();
        var sb = new StringBuilder();
        sb.Append(t.Name).Append('{');
        bool first = true;

        foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.IsDefined(typeof(IgnoreAttribute))) continue;
            string key = p.Name;
            ColumnAttribute? col = p.GetCustomAttribute<ColumnAttribute>();
            if (col is not null) key = col.Name;
            if (!first) sb.Append(',');
            first = false;
            sb.Append(key).Append('=').Append(p.GetValue(o));
        }

        foreach (FieldInfo f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (f.IsDefined(typeof(IgnoreAttribute))) continue;
            string key = f.Name;
            ColumnAttribute? col = f.GetCustomAttribute<ColumnAttribute>();
            if (col is not null) key = col.Name;
            if (!first) sb.Append(',');
            first = false;
            sb.Append(key).Append('=').Append(f.GetValue(o));
        }

        sb.Append('}');
        return sb.ToString();
    }
}

public static class Program
{
    internal static void Run()
    {
        // 1) resolve a type purely by name, then instantiate it with Activator
        Type? t = Type.GetType("ReflectSerializerSubset.Account");
        Console.WriteLine("resolved=" + (t?.FullName ?? "null"));
        object acc = Activator.CreateInstance(t!)!;

        // 2) populate through property + field reflection (string/int/enum)
        t!.GetProperty("Name")!.SetValue(acc, "alice");
        t.GetProperty("Age")!.SetValue(acc, 30);
        t.GetProperty("Status")!.SetValue(acc, Status.Active);
        t.GetField("Note")!.SetValue(acc, "vip");

        // 3) attribute-driven serialize (GetCustomAttribute<T> + IsDefined)
        Console.WriteLine(MiniSerializer.Serialize(acc));

        // 4) An attribute neither present nor reachable: the typed filter's null
        //    token must mean "no match", never "all".
        PropertyInfo age = t.GetProperty("Age")!;
        Console.WriteLine("age has Column=" + age.IsDefined(typeof(ColumnAttribute)));
        Console.WriteLine("age has Obsolete=" + age.IsDefined(typeof(ObsoleteAttribute)));

        // 5) non-generic Attribute.GetCustomAttributes array form (used as an array)
        Attribute[] onAge = Attribute.GetCustomAttributes(age, typeof(ColumnAttribute));
        Console.WriteLine("age Column count=" + onAge.Length + " name=" + ((ColumnAttribute)onAge[0]).Name);

        // 6) GetCustomAttribute<T> on the field, reading a named property off the instance
        FieldInfo note = t.GetField("Note")!;
        ColumnAttribute? nc = note.GetCustomAttribute<ColumnAttribute>();
        Console.WriteLine("note column=" + (nc?.Name ?? "?") + " order=" + (nc?.Order ?? -1));

        // 7) invoke a method by name with no args
        MethodInfo m = t.GetMethod("Summary")!;
        Console.WriteLine("summary=" + m.Invoke(acc, null));

        // 8) enum reflection through a runtime Type
        Type st = typeof(Status);
        var names = new List<string>();
        foreach (string n in Enum.GetNames(st)) names.Add(n);
        Console.WriteLine("status names=" + string.Join("|", names));

        // 9) round-trip a second instance, deserialize-style by attribute key
        object acc2 = Activator.CreateInstance(t)!;
        SetByColumn(t, acc2, "user_name", "bob");
        SetByColumn(t, acc2, "age_years", 7);
        Console.WriteLine(MiniSerializer.Serialize(acc2));
    }

    // Find a property whose [Column] name (or own name) matches, then set it.
    private static void SetByColumn(Type t, object target, string column, object value)
    {
        foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            string key = p.Name;
            ColumnAttribute? col = p.GetCustomAttribute<ColumnAttribute>();
            if (col is not null) key = col.Name;
            if (key == column) { p.SetValue(target, value); return; }
        }
    }
}
