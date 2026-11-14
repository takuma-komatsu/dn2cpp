using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeFieldHandleSubset;

internal static unsafe class Program
{
    public sealed class Target
    {
        public int Value = 23;
        private int _hidden = 31;
        public static int StaticValue = 41;
        public static readonly int ReadOnlyValue = 47;
        public int HiddenValue() => _hidden;
    }

    public struct ValueTarget { public int Value; }
    public sealed class GenericTarget<T> { public T Value; public static T StaticValue; }

    private const BindingFlags QueryFlags = BindingFlags.NonPublic | BindingFlags.Static;

    internal static void Run()
    {
        Console.WriteLine("== Runtime field handle contract ==");
        RuntimeFieldHandle handle = default;
        RuntimeFieldHandle other = default;
        Console.WriteLine("field-handle-default-value:" + (handle.Value == IntPtr.Zero && RuntimeFieldHandle.ToIntPtr(handle) == IntPtr.Zero));
        Console.WriteLine("field-handle-default-equality:" + (handle.Equals(other) && handle.Equals((object)other) && handle == other && !(handle != other) && !handle.Equals(null) && !handle.Equals(new object())));
        Console.WriteLine("field-handle-default-hash:" + (handle.GetHashCode() == HashCode.Combine(IntPtr.Zero)));
        try { RuntimeFieldHandle.FromIntPtr(IntPtr.Zero); Console.WriteLine("field-handle-default-validation:accepted"); }
        catch (ArgumentNullException) { Console.WriteLine("field-handle-default-validation:ArgumentNullException"); }
        var target = new Target();
        FieldInfo field = typeof(Target).GetField("Value");
        field.SetValue(target, 29);
        Console.WriteLine("field-handle-public-access:" + (target.Value == 29 && (int)field.GetValue(target) == 29));
        Console.WriteLine("field-handle-public-metadata:" + (field.Name == "Value" && field.DeclaringType == typeof(Target) && field.FieldType == typeof(int) && field.Attributes == FieldAttributes.Public && field.IsPublic && !field.IsStatic));
        FieldInfo hidden = typeof(Target).GetField("_hidden", BindingFlags.NonPublic | BindingFlags.Instance);
        hidden.SetValue(target, 37);
        Console.WriteLine("field-handle-private-access:" + (hidden.IsPrivate && (int)hidden.GetValue(target) == 37 && target.HiddenValue() == 37));
        FieldInfo staticField = typeof(Target).GetField("StaticValue");
        staticField.SetValue(null, 43);
        Console.WriteLine("field-handle-static-access:" + (staticField.IsStatic && (int)staticField.GetValue(null) == 43 && Target.StaticValue == 43));
        FieldInfo readOnly = typeof(Target).GetField("ReadOnlyValue");
        Console.WriteLine("field-handle-readonly-value:" + (readOnly.IsInitOnly && (int)readOnly.GetValue(null) == 47));
        try { readOnly.SetValue(null, 53); Console.WriteLine("field-handle-readonly-store:accepted"); }
        catch (FieldAccessException) { Console.WriteLine("field-handle-readonly-store:FieldAccessException"); }
        object boxed = new ValueTarget { Value = 59 };
        FieldInfo valueField = typeof(ValueTarget).GetField("Value");
        valueField.SetValue(boxed, 61);
        Console.WriteLine("field-handle-struct-access:" + ((int)valueField.GetValue(boxed) == 61 && ((ValueTarget)boxed).Value == 61));
        var generic = new GenericTarget<string> { Value = "before" };
        FieldInfo genericField = typeof(GenericTarget<string>).GetField("Value");
        genericField.SetValue(generic, "after");
        Console.WriteLine("field-handle-generic-access:" + (genericField.FieldType == typeof(string) && generic.Value == "after" && (string)genericField.GetValue(generic) == "after"));
        FieldInfo genericStatic = typeof(GenericTarget<int>).GetField("StaticValue");
        genericStatic.SetValue(null, 67);
        Console.WriteLine("field-handle-generic-static:" + ((int)genericStatic.GetValue(null) == 67 && GenericTarget<int>.StaticValue == 67 && GenericTarget<string>.StaticValue == null));
        int[] array = { 3, 5, 7, 11, 13, 17 };
        ReadOnlySpan<byte> span = [2, 4, 6, 8, 10, 12];
        Console.WriteLine("field-handle-rva-array:" + (array.Length == 6 && array[0] == 3 && array[5] == 17));
        Console.WriteLine("field-handle-rva-span:" + (span.Length == 6 && span[0] == 2 && span[5] == 12));
        RunMetadataChecks();
        Console.WriteLine("Runtime field handle contract end");
    }

    private static void CheckShape(string name, MethodInfo method, string result, bool isPrivate)
    {
        Console.WriteLine("field-handle-shape-" + name + ":" + (method.IsStatic && !method.IsGenericMethod
            && (isPrivate ? method.IsPrivate : method.IsAssembly) && method.ReturnType.FullName == result));
    }

    internal static void RunMetadataChecks()
    {
        CheckShape("GetUtf8NameInternal", typeof(RuntimeFieldHandle).GetMethod("GetUtf8NameInternal", QueryFlags, new Type[] { Type.GetType("System.RuntimeFieldHandleInternal") }), "System.Void*", true);
        CheckShape("GetAttributes", typeof(RuntimeFieldHandle).GetMethod("GetAttributes", QueryFlags, new Type[] { Type.GetType("System.RuntimeFieldHandleInternal") }), "System.Reflection.FieldAttributes", false);
        CheckShape("GetApproxDeclaringMethodTable", typeof(RuntimeFieldHandle).GetMethod("GetApproxDeclaringMethodTable", QueryFlags, new Type[] { Type.GetType("System.RuntimeFieldHandleInternal") }), "System.Runtime.CompilerServices.MethodTable*", true);
        CheckShape("IsFastPathSupported", typeof(RuntimeFieldHandle).GetMethod("IsFastPathSupported", QueryFlags, new Type[] { Type.GetType("System.Reflection.RtFieldInfo") }), "System.Boolean", false);
        CheckShape("GetInstanceFieldOffset", typeof(RuntimeFieldHandle).GetMethod("GetInstanceFieldOffset", QueryFlags, new Type[] { Type.GetType("System.Reflection.RtFieldInfo") }), "System.Int32", false);
        CheckShape("GetStaticFieldAddress", typeof(RuntimeFieldHandle).GetMethod("GetStaticFieldAddress", QueryFlags, new Type[] { Type.GetType("System.Reflection.RtFieldInfo") }), "System.IntPtr", false);
        CheckShape("GetFieldDataReference", typeof(RuntimeFieldHandle).GetMethod("GetFieldDataReference", QueryFlags, new Type[] { typeof(IntPtr), Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack"), Type.GetType("System.Runtime.CompilerServices.ByteRefOnStack") }), "System.Void", true);
        CheckShape("GetToken", typeof(RuntimeFieldHandle).GetMethod("GetToken", QueryFlags, new Type[] { typeof(IntPtr) }), "System.Int32", false);
        CheckShape("GetValueDirect", typeof(RuntimeFieldHandle).GetMethod("GetValueDirect", QueryFlags, new Type[] { typeof(IntPtr), typeof(void*), Type.GetType("System.Runtime.CompilerServices.QCallTypeHandle"), Type.GetType("System.Runtime.CompilerServices.QCallTypeHandle"), Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack") }), "System.Void", true);
        CheckShape("SetValueDirect", typeof(RuntimeFieldHandle).GetMethod("SetValueDirect", QueryFlags, new Type[] { typeof(IntPtr), typeof(void*), Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack"), Type.GetType("System.Runtime.CompilerServices.QCallTypeHandle"), Type.GetType("System.Runtime.CompilerServices.QCallTypeHandle") }), "System.Void", true);
        CheckShape("GetStaticFieldForGenericType", typeof(RuntimeFieldHandle).GetMethod("GetStaticFieldForGenericType", QueryFlags, new Type[] { Type.GetType("System.RuntimeFieldHandleInternal"), Type.GetType("System.Runtime.CompilerServices.MethodTable").MakePointerType() }), Type.GetType("System.RuntimeFieldHandleInternal").FullName, true);
        CheckShape("AcquiresContextFromThis", typeof(RuntimeFieldHandle).GetMethod("AcquiresContextFromThis", QueryFlags, new Type[] { Type.GetType("System.RuntimeFieldHandleInternal") }), "System.Boolean", false);
        CheckShape("GetLoaderAllocatorInternal", typeof(RuntimeFieldHandle).GetMethod("GetLoaderAllocatorInternal", QueryFlags, new Type[] { Type.GetType("System.RuntimeFieldHandleInternal") }), Type.GetType("System.Reflection.LoaderAllocator").FullName, true);
        CheckShape("GetEnCFieldAddr", typeof(RuntimeFieldHandle).GetMethod("GetEnCFieldAddr", QueryFlags, new Type[] { Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack"), typeof(void*) }), "System.Void*", true);
        CheckShape("GetRVAFieldInfo", typeof(RuntimeFieldHandle).GetMethod("<GetRVAFieldInfo>g____PInvoke|24_0", QueryFlags, new Type[] { Type.GetType("System.RuntimeFieldHandleInternal"), typeof(void**), typeof(uint*) }), "System.Int32", false);
        CheckShape("GetValue", typeof(RuntimeFieldHandle).GetMethod("<GetValue>g____PInvoke|30_0", QueryFlags, new Type[] { typeof(IntPtr), Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack"), Type.GetType("System.Runtime.CompilerServices.QCallTypeHandle"), Type.GetType("System.Runtime.CompilerServices.QCallTypeHandle"), typeof(int*), Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack") }), "System.Void", false);
        CheckShape("SetValue", typeof(RuntimeFieldHandle).GetMethod("<SetValue>g____PInvoke|34_0", QueryFlags, new Type[] { typeof(IntPtr), Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack"), Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack"), Type.GetType("System.Runtime.CompilerServices.QCallTypeHandle"), Type.GetType("System.Runtime.CompilerServices.QCallTypeHandle"), typeof(int*) }), "System.Void", false);
        Console.WriteLine("Runtime field handle metadata end");
    }

    private static void GetAttributesDirect() => throw new InvalidOperationException("fixture missing");

    private static void Refuse(string label, Action action)
    {
        try { action(); Console.WriteLine(label + ":accepted"); }
        catch (TargetInvocationException error) { Console.WriteLine(label + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine(label + ":" + error.GetType().Name + ":" + error.Message); }
    }

    // A CoreCLR FieldDesc InternalCall has no native body; the rewritten call refuses.
    internal static void RunNativeBoundary() => Refuse("field-handle-GetAttributes-direct", GetAttributesDirect);
}
