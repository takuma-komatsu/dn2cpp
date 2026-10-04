extern alias metadataAlias;

using System;
using System.Reflection;
using Lib = metadataAlias::AssemblyScopeCollision;

namespace AssemblyScopeCollision
{
    public static class Who
    {
        public static string Name() => "application";
    }

    public class Box
    {
        public int Number;
        public string Tag;
        public Box(int number) { Number = number; Tag = "app"; }
        public int Read() => Number;
    }

    public sealed class Generic<T>
    {
        public T Value;
        public static int Count;
        public Generic(T value) { Value = value; Count++; }
        public T Read() => Value;
        public static string Label() => "application";
    }
}

namespace MultiAssembly
{
    internal static class AssemblyScopeSubset
    {
        public static void Run()
        {
            Console.WriteLine("assembly-type-scope-begin");
            var app = new AssemblyScopeCollision.Box(17);
            var lib = new Lib.Box(29);
            var holder = new Lib.Holder(lib);
            Console.WriteLine("bodies=" + AssemblyScopeCollision.Who.Name() + "/" + Lib.Who.Name());
            Console.WriteLine("layouts=" + app.Read() + "/" + lib.Read() + "/" + lib.Wide);
            Console.WriteLine("fields=" + holder.Value.Read() + "/" + holder.Values[0].Read()
                + "/" + Lib.Holder.StaticValue.Read() + "/" + Lib.Holder.Accept(lib));
            object appObject = app;
            object libObject = lib;
            object child = new Lib.Child(31);
            Console.WriteLine("tests=" + (appObject is AssemblyScopeCollision.Box) + "/"
                + (appObject is Lib.Box) + "/" + (libObject is AssemblyScopeCollision.Box)
                + "/" + (libObject is Lib.Box) + "/" + (child is Lib.Box)
                + "/" + (child is AssemblyScopeCollision.Box));
            Type lt = typeof(Lib.Box);
            Console.WriteLine("type=" + (lt == lib.GetType()) + "/"
                + (lt == Lib.Holder.ReadType()) + "/" + (lt != typeof(AssemblyScopeCollision.Box))
                + "/" + lt.Assembly.FullName.StartsWith("MultiAssemblyAlias,"));
            Type ht = typeof(Lib.Holder);
            Console.WriteLine("signature=" + (ht.GetField("Value").FieldType == lt)
                + "/" + (ht.GetField("StaticValue").FieldType == lt)
                + "/" + (ht.GetField("Values").FieldType.GetElementType() == lt)
                + "/" + (holder.Values.GetType().GetElementType() == lt));
            MethodInfo accept = ht.GetMethod("Accept");
            Console.WriteLine("invoke-signature=" + (accept.GetParameters()[0].ParameterType == lt));
            Console.WriteLine("invoke=" + accept.Invoke(null, new object[] { lib }));
            var mixed = new Mixed<int>();
            Console.WriteLine("scope-overloads=" + mixed.Read(app) + "/" + mixed.Read(lib)
                + "/" + mixed.Generic<string>(app) + "/" + mixed.Generic<string>(lib));
            try
            {
                accept.Invoke(null, new object[] { app });
                Console.WriteLine("invoke-wrong=accepted");
            }
            catch (ArgumentException) { Console.WriteLine("invoke-wrong=ArgumentException"); }
            Console.WriteLine("assembly-type-scope-end");
        }

        private sealed class Mixed<T>
        {
            public int Read(AssemblyScopeCollision.Box value) => value.Read() + 1;
            public int Read(Lib.Box value) => value.Read() + 2;
            public int Generic<U>(AssemblyScopeCollision.Box value) => value.Read() + 3;
            public int Generic<U>(Lib.Box value) => value.Read() + 4;
        }

        [NamesType(typeof(Lib.Generic<int>))]
        private sealed class GenericTagged { }

        public static void RunClosedGenerics()
        {
            Console.WriteLine("assembly-closed-types-begin");
            var app = new AssemblyScopeCollision.Generic<int>(41);
            var lib = new Lib.Generic<int>(53);
            var appString = new AssemblyScopeCollision.Generic<string>("app-value");
            var libString = new Lib.Generic<string>("lib-value");
            Console.WriteLine("generic-values=" + app.Read() + "/" + lib.Read()
                + "/" + appString.Read() + "/" + libString.Read());
            Console.WriteLine("generic-bodies=" + AssemblyScopeCollision.Generic<int>.Label()
                + "/" + Lib.Generic<int>.Label());
            Type appType = typeof(AssemblyScopeCollision.Generic<int>);
            Type libType = typeof(Lib.Generic<int>);
            Console.WriteLine("generic-distinct=" + (appType != libType) + "/"
                + (typeof(AssemblyScopeCollision.Generic<string>) != typeof(Lib.Generic<string>))
                + "/" + (app.GetType() != lib.GetType()));
            Console.WriteLine("generic-owners=" + appType.Assembly.FullName.StartsWith("MultiAssembly,")
                + "/" + libType.Assembly.FullName.StartsWith("MultiAssemblyAlias,"));
            object libraryObject = lib;
            Console.WriteLine("generic-tests=" + (libraryObject is Lib.Generic<int>)
                + "/" + (libraryObject is AssemblyScopeCollision.Generic<int>));
            var secondLibrary = new Lib.Generic<int>(67);
            Console.WriteLine("generic-statics=" + AssemblyScopeCollision.Generic<int>.Count
                + "/" + Lib.Generic<int>.Count + "/" + secondLibrary.Read());
            object[] attributes = typeof(GenericTagged).GetCustomAttributes(typeof(NamesTypeAttribute), false);
            Type argument = ((NamesTypeAttribute)attributes[0]).Type;
            Console.WriteLine("generic-attribute=" + (argument == libType) + "/"
                + (argument != appType) + "/" + argument.Assembly.FullName.StartsWith("MultiAssemblyAlias,"));
            Console.WriteLine("assembly-closed-types-end");
        }
    }
}
