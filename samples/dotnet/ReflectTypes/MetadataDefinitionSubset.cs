using System;
using System.Reflection;
using System.Threading.Tasks;

namespace MetadataDefinitionSubset;

internal static class Program
{
    private class Owner<T>
    {
        public T Field = default!;
        public T OtherField = default!;
        public T Property { get; set; } = default!;
        public T OtherProperty { get; set; } = default!;
        public event Action Changed { add { } remove { } }
        public event Action OtherChanged { add { } remove { } }
        public Owner() { }
        public Owner(T value) { Field = value; }
        public void Method(T value) { }
        public void Other(T value) { }
        public void Generic<U>(U value) { }
        public void OtherGeneric<U>(U value) { }
        public class Nested<U> { }
    }

    private sealed class Derived : Owner<int> { }
    private class VirtualBase { public virtual void Method() { } }
    private sealed class VirtualDerived : VirtualBase { public override void Method() { } }
    private sealed class Pair<T, U> { }
    private sealed class HidesObject
    {
        public new Type GetType() => typeof(HidesObject);
    }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("metadata-definition-" + label + ":" + success);
        if (!success)
            throw new InvalidOperationException(label);
    }

    private static bool Same(MemberInfo first, MemberInfo second) =>
        first.HasSameMetadataDefinitionAs(second) && second.HasSameMetadataDefinitionAs(first);

    private static bool Different(MemberInfo first, MemberInfo second) =>
        !first.HasSameMetadataDefinitionAs(second) && !second.HasSameMetadataDefinitionAs(first);

    private static Exception? Error(Action action)
    {
        try { action(); }
        catch (Exception error) { return error; }
        return null;
    }

    private static MemberInfo[] Members(Type owner) => new MemberInfo[]
    {
        owner,
        owner.GetConstructor(Type.EmptyTypes)!,
        owner.GetMethod("Method")!,
        owner.GetField("Field")!,
        owner.GetProperty("Property")!,
        owner.GetEvent("Changed")!,
    };

    internal static void Run()
    {
        Console.WriteLine("== member metadata definition identity ==");
        MemberInfo[] integers = Members(typeof(Owner<int>));
        MemberInfo[] strings = Members(typeof(Owner<string>));
        MemberInfo[] definitions = Members(typeof(Owner<>));
        string[] kinds = { "type", "constructor", "method", "field", "property", "event" };
        for (int i = 0; i < integers.Length; i++)
        {
            MemberInfo member = integers[i];
            Check(kinds[i] + "-self", Same(member, member));
            Check(kinds[i] + "-closed", !ReferenceEquals(member, strings[i]) && Same(member, strings[i]));
            Check(kinds[i] + "-definition", !ReferenceEquals(member, definitions[i]) && Same(member, definitions[i]));
            Exception? error = Error(() => member.HasSameMetadataDefinitionAs(null!));
            Check(kinds[i] + "-null", error is ArgumentNullException argument && argument.ParamName == "other");
            Check(kinds[i] + "-kind", Different(member, integers[(i + 1) % integers.Length]));
        }

        MethodInfo generic = typeof(Owner<int>).GetMethod("Generic")!;
        MethodInfo genericString = typeof(Owner<string>).GetMethod("Generic")!;
        MethodInfo genericDefinition = typeof(Owner<>).GetMethod("Generic")!;
        MethodInfo closedInteger = generic.MakeGenericMethod(typeof(int));
        MethodInfo closedString = genericString.MakeGenericMethod(typeof(string));
        Check("method-instantiation", !ReferenceEquals(closedInteger, closedString) && Same(closedInteger, closedString));
        Check("method-definition-view", !ReferenceEquals(generic, closedInteger) && Same(generic, closedInteger));
        Check("method-other", Different(integers[2], typeof(Owner<int>).GetMethod("Other")!));
        Check("constructor-other", Different(integers[1], typeof(Owner<int>).GetConstructor(new[] { typeof(int) })!));
        Check("field-other", Different(integers[3], typeof(Owner<int>).GetField("OtherField")!));
        Check("property-other", Different(integers[4], typeof(Owner<int>).GetProperty("OtherProperty")!));
        Check("event-other", Different(integers[5], typeof(Owner<int>).GetEvent("OtherChanged")!));
        Check("method-override", Different(typeof(VirtualBase).GetMethod("Method")!, typeof(VirtualDerived).GetMethod("Method")!));
        Check("nested-closed", Same(typeof(Owner<int>.Nested<string>), typeof(Owner<string>.Nested<int>)));
        Check("nested-other", Different(typeof(Owner<int>.Nested<string>), typeof(Owner<int>)));

        MemberInfo[] inherited = Members(typeof(Derived));
        for (int i = 2; i < integers.Length; i++)
            Check(kinds[i] + "-reflected", !ReferenceEquals(integers[i], inherited[i]) && Same(integers[i], inherited[i]));

        Type typeParameter = typeof(Owner<>).GetGenericArguments()[0];
        Type methodParameter = generic.GetGenericArguments()[0];
        Check("formal-self", Same(typeParameter, typeParameter) && Same(methodParameter, methodParameter));
        Check("formal-owner-close", Same(methodParameter, genericString.GetGenericArguments()[0]));
        Check("formal-definition", Same(methodParameter, genericDefinition.GetGenericArguments()[0]));
        Check("formal-method-owner", Different(methodParameter, typeof(Owner<int>).GetMethod("OtherGeneric")!.GetGenericArguments()[0]));
        Check("formal-type-method", Different(typeParameter, methodParameter));
        Check("formal-nested-owner", Different(typeParameter, typeof(Owner<>.Nested<>).GetGenericArguments()[0]));
        Check("formal-nested-position", Different(typeof(Owner<>.Nested<>).GetGenericArguments()[0], typeof(Owner<>.Nested<>).GetGenericArguments()[1]));
        Type partial = typeof(Pair<,>).MakeGenericType(typeof(string), typeParameter);
        Check("partial-definition", Same(partial, typeof(Pair<,>)));
        Check("partial-closed", Same(partial, typeof(Pair<string, int>)));

        Check("builtin-self", Same(typeof(object), typeof(object)) && Same(typeof(int), typeof(int)) && Same(typeof(string), typeof(string)));
        Check("builtin-distinct", Different(typeof(object), typeof(int)) && Different(typeof(int), typeof(string)));
        Check("builtin-reflection-bases", Different(typeof(Type), typeof(MemberInfo)));
        Check("builtin-generic", Same(typeof(Task<int>), typeof(Task<string>)));
        Check("builtin-generic-definition", Same(typeof(Task<int>), typeof(Task<>)));
        Check("builtin-user", Different(typeof(object), typeof(Owner<int>)));
        FieldInfo emptyField = typeof(string).GetField("Empty")!;
        Check("builtin-field-self", Same(emptyField, emptyField));
        Check("builtin-field-user", Different(emptyField, integers[3]));

        Type pointer = typeof(int).MakePointerType();
        Type byref = typeof(int).MakeByRefType();
        Check("compound-array", Same(typeof(int[]), typeof(string[])) && Same(typeof(int[]), typeof(int[,])));
        Check("compound-pointer", Same(pointer, typeof(string).MakePointerType()));
        Check("compound-byref", Same(byref, typeof(string).MakeByRefType()));
        Check("compound-cross-shape", Same(typeof(int[]), pointer) && Same(pointer, byref));
        Type[] coreModuleTypes = { typeof(object[]), typeof(string[]), typeof(bool[]), typeof(char[]), typeof(byte[]), typeof(sbyte[]),
            typeof(short[]), typeof(ushort[]), typeof(int[]), typeof(uint[]), typeof(long[]), typeof(ulong[]), typeof(IntPtr[]), typeof(UIntPtr[]),
            typeof(float[]), typeof(double[]) };
        bool coreModules = true;
        for (int i = 0; i < coreModuleTypes.Length; i++)
            coreModules &= Same(coreModuleTypes[i], typeof(int[]));
        Check("compound-core-modules", coreModules && Same(typeof(void).MakePointerType(), typeof(int).MakePointerType()));
        Check("compound-recorded-module", Different(typeof(System.IO.MemoryMappedFiles.MemoryMappedFile[]), typeof(int[])));
        Check("compound-module", typeof(int[]).Module != typeof(Owner<int>[]).Module
            && Different(typeof(int[]), typeof(Owner<int>[])));
        Check("compound-definition", Different(typeof(int[]), typeof(int)));
        Check("compound-formal", Different(typeof(int[]), typeParameter));

        MethodInfo objectGetType = typeof(object).GetMethod("GetType")!;
        MethodInfo inheritedGetType = typeof(Owner<int>).GetMethod("GetType")!;
        MethodInfo objectToString = typeof(object).GetMethod("ToString")!;
        Check("descriptor-reflected", !ReferenceEquals(objectGetType, inheritedGetType) && Same(objectGetType, inheritedGetType));
        Check("descriptor-distinct", Different(objectGetType, objectToString));
        Check("descriptor-same-name-different-owner", Different(objectGetType, typeof(Exception).GetMethod("GetType")!));
        Check("descriptor-owner", Different(objectGetType, typeof(HidesObject).GetMethod("GetType")!));
        Check("module-method", objectGetType.Module != integers[2].Module && Different(objectGetType, integers[2]));
        MethodInfo sizeOf = typeof(System.Runtime.CompilerServices.Unsafe).GetMethod("SizeOf")!;
        Check("descriptor-generic-instance", Same(sizeOf.MakeGenericMethod(typeof(int)), sizeOf.MakeGenericMethod(typeof(string))));
        Check("descriptor-generic-definition", Same(sizeOf, sizeOf.MakeGenericMethod(typeof(int))));
        Check("descriptor-null", Error(() => objectGetType.HasSameMetadataDefinitionAs(null!)) is ArgumentNullException descriptorNull
            && descriptorNull.ParamName == "other");
        Func<MemberInfo, bool> bound = integers[2].HasSameMetadataDefinitionAs;
        Check("virtual-delegate", bound(strings[2]) && !bound(typeof(Owner<int>).GetMethod("Other")!));
        Check("virtual-delegate-kind", !bound(typeof(int)));
        Check("virtual-delegate-null", Error(() => bound(null!)) is ArgumentNullException delegateNull
            && delegateNull.ParamName == "other");
        Check("null-receiver", Error(() => ((MemberInfo)null!).HasSameMetadataDefinitionAs(typeof(int))) is NullReferenceException);
        Check("null-receiver-other", Error(() => ((MemberInfo)null!).HasSameMetadataDefinitionAs(null!)) is NullReferenceException);
        Console.WriteLine("member metadata definition identity end");
    }
}
