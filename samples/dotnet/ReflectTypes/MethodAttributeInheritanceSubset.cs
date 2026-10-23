using System;
using System.Reflection;

namespace MethodAttributeInheritanceSubset;

static class Program
{
    public static void Run()
    {
        Console.WriteLine("== inherited method attributes ==");
        Read(typeof(Hud).GetMethod(nameof(Hud.Press))!, "override");
        Read(typeof(OtherHud).GetMethod(nameof(OtherHud.Press))!, "other-override");
        Read(typeof(HiddenHud).GetMethod(nameof(HiddenHud.Press))!, "new-slot");
        Read(typeof(Hud).GetMethod(nameof(Hud.Generic))!, "generic-method");
        var method = typeof(Hud).GetMethod(nameof(Hud.Press))!;
        Console.WriteLine("static=" + Names(Attribute.GetCustomAttributes(method, typeof(InputAttribute))));
        Console.WriteLine("extension=" + Names(method.GetCustomAttributes<InputAttribute>()));
        Console.WriteLine("single=" + method.GetCustomAttribute<SingleAttribute>()!.Name);
        string data = "";
        foreach (var attribute in method.GetCustomAttributesData())
            if (typeof(InputAttribute).IsAssignableFrom(attribute.AttributeType))
                data += (data.Length == 0 ? "" : ",") + attribute.AttributeType.Name;
        Console.WriteLine("declared-data=" + data);
        var first = method.GetCustomAttributes(typeof(InputAttribute), true);
        var second = method.GetCustomAttributes(typeof(InputAttribute), true);
        Console.WriteLine("fresh=" + !ReferenceEquals(first[0], second[0]));
        Console.WriteLine("selected-base=" + typeof(SelectedChild).GetMethod(nameof(SelectedChild.Run))!
            .GetCustomAttributes(typeof(SelectedAttribute), true).Length);
        Console.WriteLine("non-inherited=" + typeof(SelectedChild).GetMethod(nameof(SelectedChild.Run))!
            .GetCustomAttributes(typeof(NonInheritedAttribute), true).Length);
        Console.WriteLine("type-inherited=" + typeof(SelectedChild).GetCustomAttributes(typeof(SelectedAttribute), true).Length);
        Console.WriteLine("type-declared=" + typeof(SelectedChild).GetCustomAttributes(typeof(SelectedAttribute), false).Length);
        Console.WriteLine("type-non-inherited=" + typeof(SelectedChild).GetCustomAttributes(typeof(NonInheritedAttribute), true).Length);
        Console.WriteLine("inherited method attributes end");
    }

    private static void Read(MethodInfo method, string name)
    {
        InputAttribute.Constructed = 0;
        Console.WriteLine(name + ":defined=" + method.IsDefined(typeof(InputAttribute), true)
            + ":constructors=" + InputAttribute.Constructed);
        Console.WriteLine(name + ":declared=" + Names(method.GetCustomAttributes(typeof(InputAttribute), false)));
        Console.WriteLine(name + ":inherited=" + Names(method.GetCustomAttributes(typeof(InputAttribute), true)));
    }

    private static string Names(System.Collections.IEnumerable attributes)
    {
        string text = "";
        foreach (InputAttribute attribute in attributes)
            text += (text.Length == 0 ? "" : ",") + attribute.Name;
        return text;
    }

    abstract class InputAttribute : Attribute
    {
        public static int Constructed;
        public readonly string Name;
        protected InputAttribute(string name) { Name = name; Constructed++; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    class KeyAttribute : InputAttribute
    {
        public KeyAttribute(string name) : base(name) { }
    }

    // Runtime queries use default usage when the concrete attribute has no usage row.
    sealed class KeyDownAttribute : KeyAttribute
    {
        public KeyDownAttribute(string name) : base(name) { }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    sealed class LocalAttribute : InputAttribute
    {
        public LocalAttribute(string name) : base(name) { }
    }

    sealed class SingleAttribute : InputAttribute
    {
        public SingleAttribute(string name) : base(name) { }
    }

    class Base<T>
    {
        [KeyDown("base-key"), Key("base-multi"), Single("base-single"), Local("base-local")]
        public virtual void Press() { }
        [KeyDown("base-generic")]
        public virtual void Generic<U>(U value) { }
    }

    class Middle<T> : Base<T>
    {
        [KeyDown("middle-key"), Key("middle-multi"), Single("middle-single")]
        public override void Press() { }
    }

    sealed class Hud : Middle<string>
    {
        [KeyDown("hud-key"), Key("hud-multi")]
        public override void Press() { }
        public override void Generic<U>(U value) { }
    }

    sealed class OtherHud : Base<int>
    {
        public override void Press() { }
    }

    class HiddenHud : Base<string>
    {
        public new virtual void Press() { }
    }

    sealed class SelectedAttribute : Attribute
    {
        public SelectedAttribute() { Console.WriteLine("selected attribute constructed"); }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
    sealed class NonInheritedAttribute : Attribute
    {
        public NonInheritedAttribute() { Console.WriteLine("non-inherited attribute constructed"); }
    }

    [Selected, NonInherited]
    class SelectedBase
    {
        [Selected, NonInherited]
        public virtual void Run() { }
    }

    sealed class SelectedChild : SelectedBase
    {
        public override void Run() { }
    }
}
