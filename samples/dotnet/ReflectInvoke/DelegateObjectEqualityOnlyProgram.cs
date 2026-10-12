using System;
using System.Globalization;

class OverrideCase
{
    public override string ToString() => "override";
    public Func<string> DirectBase() => base.ToString;
}

class Program
{
    static void Check(string name, object target, Func<string> compiled)
    {
        var reflected = (Func<string>)typeof(object).GetMethod("ToString", Type.EmptyTypes)
            .CreateDelegate(typeof(Func<string>), target);
        Console.WriteLine(name + "-values=" + (compiled() == reflected()));
        Console.WriteLine(name + "-equality=" + compiled.Equals(reflected) + "/" + reflected.Equals(compiled)
            + "/" + (compiled.GetHashCode() == reflected.GetHashCode()));
        var remaining = Delegate.Remove(compiled + reflected, compiled).GetInvocationList();
        Console.WriteLine(name + "-remove=" + ReferenceEquals(remaining[0], compiled) + "/" + ((Func<string>)remaining[0])());
    }

    static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== equality without Method ==");
        var root = new object();
        Check("Object", root, root.ToString);
        var target = new OverrideCase();
        Check("Override", target, target.ToString);
        var directBase = target.DirectBase();
        var reflected = (Func<string>)typeof(object).GetMethod("ToString", Type.EmptyTypes)
            .CreateDelegate(typeof(Func<string>), target);
        Console.WriteLine("base-equality=" + directBase.Equals(reflected) + "/" + reflected.Equals(directBase));
        var remaining = Delegate.Remove(directBase + reflected, directBase).GetInvocationList();
        Console.WriteLine("base-remove=" + ReferenceEquals(remaining[0], reflected) + "/" + ((Func<string>)remaining[0])());
        Func<object, bool> equals = root.Equals;
        var reflectedEquals = (Func<object, bool>)typeof(object).GetMethod("Equals", new[] { typeof(object) })
            .CreateDelegate(typeof(Func<object, bool>), root);
        Console.WriteLine("Object-Equals=" + equals.Equals(reflectedEquals) + "/" + reflectedEquals.Equals(equals)
            + "/" + (equals.GetHashCode() == reflectedEquals.GetHashCode()) + "/" + equals(root) + "/" + reflectedEquals(root));
        Func<int> hash = root.GetHashCode;
        var reflectedHash = (Func<int>)typeof(object).GetMethod("GetHashCode", Type.EmptyTypes)
            .CreateDelegate(typeof(Func<int>), root);
        Console.WriteLine("Object-GetHashCode=" + hash.Equals(reflectedHash) + "/" + reflectedHash.Equals(hash)
            + "/" + (hash.GetHashCode() == reflectedHash.GetHashCode()) + "/" + (hash() == reflectedHash()));
        Console.WriteLine("equality without Method end");
    }
}
