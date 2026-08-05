#nullable enable
// SUBJECT: MemberInfo.ReflectedType. A member handle carries the type the QUERY
// was made on, distinct from DeclaringType for an inherited member, and handle
// interning is keyed (row, reflectedType) — .NET's own member-cache shape. So
// typeof(D).GetMethod(m) and typeof(Base).GetMethod(m) are different instances
// comparing unequal, while a repeated query on one type interns.
//
// The propagation rules pinned here: a delegate's .Method and
// GetGenericMethodDefinition normalize to DECLARING; MakeGenericMethod and
// PropertyInfo.GetGetMethod propagate the receiver's reflected type;
// ParameterInfo.Member is the originating instance; a ctor is never inherited,
// so its reflected type is its declaring type.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ReflectedTypeSubset
{
    public class Base
    {
        public int F;
        public string P { get; set; } = "p";
        public string M() => "m";
        public virtual string V() => "base-v";
        // Inherited virtual with NO override anywhere: the GetBaseDefinition
        // renormalization case.
        public virtual string W() => "base-w";
        public T Echo<T>(T v) => v;
    }

    public class D : Base
    {
        public override string V() => "d-v";
    }

    internal static class Program
    {
        internal static void Run()
        {
            Console.WriteLine("-- reflected-type --");
            var b = new Base();
            // Reach Echo<int> (the AOT image carries only reached instantiation
            // rows) and D's ctor + override (so GetConstructor finds a row).
            Console.WriteLine(b.Echo(7) + " " + new D().V());

            MethodInfo mD = typeof(D).GetMethod("M")!;
            MethodInfo mB = typeof(Base).GetMethod("M")!;
            Console.WriteLine(mD.ReflectedType!.Name + " " + mD.DeclaringType!.Name);
            Console.WriteLine(mB.ReflectedType!.Name + " " + mB.DeclaringType!.Name);
            Console.WriteLine((mD == mB) + " " + mD.Equals(mB));
            Console.WriteLine((mD == typeof(D).GetMethod("M"))
                + " " + ReferenceEquals(mD, typeof(D).GetMethod("M")));
            var set = new HashSet<MemberInfo> { mD, mB };
            Console.WriteLine(set.Count);

            // A serializer's member-selection shape: a second enumeration's members
            // are found by Contains over the first's, which needs the interning.
            var first = new List<MemberInfo>(typeof(D).GetMethods());
            bool allContained = true;
            foreach (var m in typeof(D).GetMethods())
                if (!first.Contains(m)) { allContained = false; break; }
            Console.WriteLine(allContained);

            // The virtual override: declared on D itself, so the two queries
            // wrap different rows outright.
            MethodInfo vD = typeof(D).GetMethod("V")!;
            MethodInfo vB = typeof(Base).GetMethod("V")!;
            Console.WriteLine(vD.ReflectedType!.Name + " " + vD.DeclaringType!.Name
                + " " + vB.ReflectedType!.Name + " " + vB.DeclaringType!.Name + " " + (vD == vB));
            Console.WriteLine((vD.GetBaseDefinition() == vB) + " "
                + vD.GetBaseDefinition().ReflectedType!.Name);
            // Inherited virtual, no override: a derived-reflected receiver
            // renormalizes to the declaring-typed instance.
            Console.WriteLine(typeof(D).GetMethod("W")!.GetBaseDefinition().ReflectedType!.Name);

            // delegate.Method: declaring-typed even when created from the
            // derived-reflected handle. An IL-constructed delegate's Method has no
            // metadata back-reference, so only the reflection-bound form is diffable.
            var del = (Func<string>)mD.CreateDelegate(typeof(Func<string>), b);
            Console.WriteLine(del() + " " + (del.Method == mB) + " " + (del.Method == mD)
                + " " + del.Method.ReflectedType!.Name);

            // MakeGenericMethod propagates reflected; GetGenericMethodDefinition
            // normalizes to declaring.
            MethodInfo echoD = typeof(D).GetMethod("Echo")!;
            MethodInfo closed = echoD.MakeGenericMethod(typeof(int));
            Console.WriteLine(closed.ReflectedType!.Name + " "
                + closed.GetGenericMethodDefinition().ReflectedType!.Name);

            // ParameterInfo.Member is the originating instance.
            Console.WriteLine(ReferenceEquals(echoD.GetParameters()[0].Member, echoD));

            // Field and property: same split, and the getter inherits the
            // property handle's reflected type.
            FieldInfo fD = typeof(D).GetField("F")!;
            FieldInfo fB = typeof(Base).GetField("F")!;
            Console.WriteLine(fD.ReflectedType!.Name + " " + fD.DeclaringType!.Name
                + " " + (fD == fB) + " " + fD.Equals(fB));
            PropertyInfo pD = typeof(D).GetProperty("P")!;
            PropertyInfo pB = typeof(Base).GetProperty("P")!;
            Console.WriteLine(pD.ReflectedType!.Name + " " + pD.DeclaringType!.Name
                + " " + (pD == pB));
            Console.WriteLine(pD.GetGetMethod()!.ReflectedType!.Name + " "
                + (pD.GetGetMethod() == typeof(D).GetMethod("get_P")));

            // A ConstructorInfo: never inherited, so reflected == declaring == D.
            ConstructorInfo cD = typeof(D).GetConstructor(new Type[0])!;
            Console.WriteLine(cD.ReflectedType!.Name + " " + cD.DeclaringType!.Name);
        }
    }
}
