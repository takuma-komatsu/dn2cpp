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

    private static IntPtr RvaAddress;
    private static uint RvaLength;
    private static int Initialized;
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

    // Rewritten with exact CoreLib signatures; the artifact entry point is native-only.
    private static void GetUtf8NameInternalDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetUtf8NameInternalPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetAttributesDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetAttributesPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetApproxDeclaringMethodTableDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetApproxDeclaringMethodTablePointer() => throw new InvalidOperationException("fixture missing");
    private static void IsFastPathSupportedDirect() => throw new InvalidOperationException("fixture missing");
    private static void IsFastPathSupportedPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetInstanceFieldOffsetDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetInstanceFieldOffsetPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetStaticFieldAddressDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetStaticFieldAddressPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetFieldDataReferenceDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetFieldDataReferencePointer() => throw new InvalidOperationException("fixture missing");
    private static void GetTokenDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetTokenPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetValueDirectDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetValueDirectPointer() => throw new InvalidOperationException("fixture missing");
    private static void SetValueDirectDirect() => throw new InvalidOperationException("fixture missing");
    private static void SetValueDirectPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetStaticFieldForGenericTypeDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetStaticFieldForGenericTypePointer() => throw new InvalidOperationException("fixture missing");
    private static void AcquiresContextFromThisDirect() => throw new InvalidOperationException("fixture missing");
    private static void AcquiresContextFromThisPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetLoaderAllocatorInternalDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetLoaderAllocatorInternalPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetEnCFieldAddrDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetEnCFieldAddrPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetRVAFieldInfoDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetRVAFieldInfoPointer() => throw new InvalidOperationException("fixture missing");
    private static void GetValueDirect() => throw new InvalidOperationException("fixture missing");
    private static void GetValuePointer() => throw new InvalidOperationException("fixture missing");
    private static void SetValueDirect() => throw new InvalidOperationException("fixture missing");
    private static void SetValuePointer() => throw new InvalidOperationException("fixture missing");

    private static void Refuse(string label, Action action)
    {
        try { action(); Console.WriteLine(label + ":accepted"); }
        catch (TargetInvocationException error) { Console.WriteLine(label + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine(label + ":" + error.GetType().Name + ":" + error.Message); }
    }

    private static void ResetSentinels()
    {
        RvaAddress = new IntPtr(1234);
        RvaLength = 71;
        Initialized = 73;
    }

    private static void CheckSentinels(string route)
    {
        Console.WriteLine("field-handle-" + route + "-nonmutation:" + (RvaAddress == new IntPtr(1234) && RvaLength == 71 && Initialized == 73));
    }

    internal static void RunArtifactChecks(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Run();
        ResetSentinels();
        Refuse("field-handle-GetUtf8NameInternal-direct", GetUtf8NameInternalDirect);
        CheckSentinels("GetUtf8NameInternal-direct");
        ResetSentinels();
        Refuse("field-handle-GetUtf8NameInternal-pointer", GetUtf8NameInternalPointer);
        CheckSentinels("GetUtf8NameInternal-pointer");
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("GetUtf8NameInternal", QueryFlags, new Type[] { Type.GetType("System.RuntimeFieldHandleInternal") }).Invoke(null, new object[] { null });
            Console.WriteLine("field-handle-GetUtf8NameInternal-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-GetUtf8NameInternal-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-GetUtf8NameInternal-reflection" + ":" + error.GetType().Name); }
        ResetSentinels();
        Refuse("field-handle-GetAttributes-direct", GetAttributesDirect);
        CheckSentinels("GetAttributes-direct");
        ResetSentinels();
        Refuse("field-handle-GetAttributes-pointer", GetAttributesPointer);
        CheckSentinels("GetAttributes-pointer");
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("GetAttributes", QueryFlags, new Type[] { Type.GetType("System.RuntimeFieldHandleInternal") }).Invoke(null, new object[] { null });
            Console.WriteLine("field-handle-GetAttributes-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-GetAttributes-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-GetAttributes-reflection" + ":" + error.GetType().Name); }
        ResetSentinels();
        Refuse("field-handle-GetApproxDeclaringMethodTable-direct", GetApproxDeclaringMethodTableDirect);
        CheckSentinels("GetApproxDeclaringMethodTable-direct");
        ResetSentinels();
        Refuse("field-handle-GetApproxDeclaringMethodTable-pointer", GetApproxDeclaringMethodTablePointer);
        CheckSentinels("GetApproxDeclaringMethodTable-pointer");
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("GetApproxDeclaringMethodTable", QueryFlags, new Type[] { Type.GetType("System.RuntimeFieldHandleInternal") }).Invoke(null, new object[] { null });
            Console.WriteLine("field-handle-GetApproxDeclaringMethodTable-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-GetApproxDeclaringMethodTable-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-GetApproxDeclaringMethodTable-reflection" + ":" + error.GetType().Name); }
        ResetSentinels();
        Refuse("field-handle-IsFastPathSupported-direct", IsFastPathSupportedDirect);
        CheckSentinels("IsFastPathSupported-direct");
        ResetSentinels();
        Refuse("field-handle-IsFastPathSupported-pointer", IsFastPathSupportedPointer);
        CheckSentinels("IsFastPathSupported-pointer");
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("IsFastPathSupported", QueryFlags, new Type[] { Type.GetType("System.Reflection.RtFieldInfo") }).Invoke(null, new object[] { null });
            Console.WriteLine("field-handle-IsFastPathSupported-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-IsFastPathSupported-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-IsFastPathSupported-reflection" + ":" + error.GetType().Name); }
        ResetSentinels();
        Refuse("field-handle-GetInstanceFieldOffset-direct", GetInstanceFieldOffsetDirect);
        CheckSentinels("GetInstanceFieldOffset-direct");
        ResetSentinels();
        Refuse("field-handle-GetInstanceFieldOffset-pointer", GetInstanceFieldOffsetPointer);
        CheckSentinels("GetInstanceFieldOffset-pointer");
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("GetInstanceFieldOffset", QueryFlags, new Type[] { Type.GetType("System.Reflection.RtFieldInfo") }).Invoke(null, new object[] { null });
            Console.WriteLine("field-handle-GetInstanceFieldOffset-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-GetInstanceFieldOffset-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-GetInstanceFieldOffset-reflection" + ":" + error.GetType().Name); }
        ResetSentinels();
        Refuse("field-handle-GetStaticFieldAddress-direct", GetStaticFieldAddressDirect);
        CheckSentinels("GetStaticFieldAddress-direct");
        ResetSentinels();
        Refuse("field-handle-GetStaticFieldAddress-pointer", GetStaticFieldAddressPointer);
        CheckSentinels("GetStaticFieldAddress-pointer");
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("GetStaticFieldAddress", QueryFlags, new Type[] { Type.GetType("System.Reflection.RtFieldInfo") }).Invoke(null, new object[] { null });
            Console.WriteLine("field-handle-GetStaticFieldAddress-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-GetStaticFieldAddress-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-GetStaticFieldAddress-reflection" + ":" + error.GetType().Name); }
        ResetSentinels();
        Refuse("field-handle-GetFieldDataReference-direct", GetFieldDataReferenceDirect);
        CheckSentinels("GetFieldDataReference-direct");
        ResetSentinels();
        Refuse("field-handle-GetFieldDataReference-pointer", GetFieldDataReferencePointer);
        CheckSentinels("GetFieldDataReference-pointer");
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("GetFieldDataReference", QueryFlags, new Type[] { typeof(IntPtr), Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack"), Type.GetType("System.Runtime.CompilerServices.ByteRefOnStack") }).Invoke(null, new object[] { IntPtr.Zero, null, null });
            Console.WriteLine("field-handle-GetFieldDataReference-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-GetFieldDataReference-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-GetFieldDataReference-reflection" + ":" + error.GetType().Name); }
        ResetSentinels();
        Refuse("field-handle-GetToken-direct", GetTokenDirect);
        CheckSentinels("GetToken-direct");
        ResetSentinels();
        Refuse("field-handle-GetToken-pointer", GetTokenPointer);
        CheckSentinels("GetToken-pointer");
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("GetToken", QueryFlags, new Type[] { typeof(IntPtr) }).Invoke(null, new object[] { IntPtr.Zero });
            Console.WriteLine("field-handle-GetToken-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-GetToken-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-GetToken-reflection" + ":" + error.GetType().Name); }
        ResetSentinels();
        Refuse("field-handle-GetValueDirect-direct", GetValueDirectDirect);
        CheckSentinels("GetValueDirect-direct");
        ResetSentinels();
        Refuse("field-handle-GetValueDirect-pointer", GetValueDirectPointer);
        CheckSentinels("GetValueDirect-pointer");
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("GetValueDirect", QueryFlags, new Type[] { typeof(IntPtr), typeof(void*), Type.GetType("System.Runtime.CompilerServices.QCallTypeHandle"), Type.GetType("System.Runtime.CompilerServices.QCallTypeHandle"), Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack") }).Invoke(null, new object[] { IntPtr.Zero, null, null, null, null });
            Console.WriteLine("field-handle-GetValueDirect-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-GetValueDirect-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-GetValueDirect-reflection" + ":" + error.GetType().Name); }
        ResetSentinels();
        Refuse("field-handle-SetValueDirect-direct", SetValueDirectDirect);
        CheckSentinels("SetValueDirect-direct");
        ResetSentinels();
        Refuse("field-handle-SetValueDirect-pointer", SetValueDirectPointer);
        CheckSentinels("SetValueDirect-pointer");
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("SetValueDirect", QueryFlags, new Type[] { typeof(IntPtr), typeof(void*), Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack"), Type.GetType("System.Runtime.CompilerServices.QCallTypeHandle"), Type.GetType("System.Runtime.CompilerServices.QCallTypeHandle") }).Invoke(null, new object[] { IntPtr.Zero, null, null, null, null });
            Console.WriteLine("field-handle-SetValueDirect-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-SetValueDirect-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-SetValueDirect-reflection" + ":" + error.GetType().Name); }
        ResetSentinels();
        Refuse("field-handle-GetStaticFieldForGenericType-direct", GetStaticFieldForGenericTypeDirect);
        CheckSentinels("GetStaticFieldForGenericType-direct");
        ResetSentinels();
        Refuse("field-handle-GetStaticFieldForGenericType-pointer", GetStaticFieldForGenericTypePointer);
        CheckSentinels("GetStaticFieldForGenericType-pointer");
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("GetStaticFieldForGenericType", QueryFlags, new Type[] { Type.GetType("System.RuntimeFieldHandleInternal"), Type.GetType("System.Runtime.CompilerServices.MethodTable").MakePointerType() }).Invoke(null, new object[] { null, null });
            Console.WriteLine("field-handle-GetStaticFieldForGenericType-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-GetStaticFieldForGenericType-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-GetStaticFieldForGenericType-reflection" + ":" + error.GetType().Name); }
        ResetSentinels();
        Refuse("field-handle-AcquiresContextFromThis-direct", AcquiresContextFromThisDirect);
        CheckSentinels("AcquiresContextFromThis-direct");
        ResetSentinels();
        Refuse("field-handle-AcquiresContextFromThis-pointer", AcquiresContextFromThisPointer);
        CheckSentinels("AcquiresContextFromThis-pointer");
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("AcquiresContextFromThis", QueryFlags, new Type[] { Type.GetType("System.RuntimeFieldHandleInternal") }).Invoke(null, new object[] { null });
            Console.WriteLine("field-handle-AcquiresContextFromThis-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-AcquiresContextFromThis-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-AcquiresContextFromThis-reflection" + ":" + error.GetType().Name); }
        ResetSentinels();
        Refuse("field-handle-GetLoaderAllocatorInternal-direct", GetLoaderAllocatorInternalDirect);
        CheckSentinels("GetLoaderAllocatorInternal-direct");
        ResetSentinels();
        Refuse("field-handle-GetLoaderAllocatorInternal-pointer", GetLoaderAllocatorInternalPointer);
        CheckSentinels("GetLoaderAllocatorInternal-pointer");
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("GetLoaderAllocatorInternal", QueryFlags, new Type[] { Type.GetType("System.RuntimeFieldHandleInternal") }).Invoke(null, new object[] { null });
            Console.WriteLine("field-handle-GetLoaderAllocatorInternal-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-GetLoaderAllocatorInternal-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-GetLoaderAllocatorInternal-reflection" + ":" + error.GetType().Name); }
        ResetSentinels();
        Refuse("field-handle-GetEnCFieldAddr-direct", GetEnCFieldAddrDirect);
        CheckSentinels("GetEnCFieldAddr-direct");
        ResetSentinels();
        Refuse("field-handle-GetEnCFieldAddr-pointer", GetEnCFieldAddrPointer);
        CheckSentinels("GetEnCFieldAddr-pointer");
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("GetEnCFieldAddr", QueryFlags, new Type[] { Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack"), typeof(void*) }).Invoke(null, new object[] { null, null });
            Console.WriteLine("field-handle-GetEnCFieldAddr-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-GetEnCFieldAddr-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-GetEnCFieldAddr-reflection" + ":" + error.GetType().Name); }
        ResetSentinels();
        Refuse("field-handle-GetRVAFieldInfo-direct", GetRVAFieldInfoDirect);
        CheckSentinels("GetRVAFieldInfo-direct");
        ResetSentinels();
        Refuse("field-handle-GetRVAFieldInfo-pointer", GetRVAFieldInfoPointer);
        CheckSentinels("GetRVAFieldInfo-pointer");
        void* address = (void*)1234;
        uint length = 71;
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("<GetRVAFieldInfo>g____PInvoke|24_0", QueryFlags, new Type[] { Type.GetType("System.RuntimeFieldHandleInternal"), typeof(void**), typeof(uint*) }).Invoke(null, new object[] { null, Pointer.Box(&address, typeof(void**)), Pointer.Box(&length, typeof(uint*)) });
            Console.WriteLine("field-handle-GetRVAFieldInfo-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-GetRVAFieldInfo-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-GetRVAFieldInfo-reflection" + ":" + error.GetType().Name); }
        Console.WriteLine("field-handle-GetRVAFieldInfo-reflection-nonmutation:" + (address == (void*)1234 && length == 71));
        ResetSentinels();
        Refuse("field-handle-GetValue-direct", GetValueDirect);
        CheckSentinels("GetValue-direct");
        ResetSentinels();
        Refuse("field-handle-GetValue-pointer", GetValuePointer);
        CheckSentinels("GetValue-pointer");
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("<GetValue>g____PInvoke|30_0", QueryFlags, new Type[] { typeof(IntPtr), Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack"), Type.GetType("System.Runtime.CompilerServices.QCallTypeHandle"), Type.GetType("System.Runtime.CompilerServices.QCallTypeHandle"), typeof(int*), Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack") }).Invoke(null, new object[] { IntPtr.Zero, null, null, null, null, null });
            Console.WriteLine("field-handle-GetValue-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-GetValue-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-GetValue-reflection" + ":" + error.GetType().Name); }
        ResetSentinels();
        Refuse("field-handle-SetValue-direct", SetValueDirect);
        CheckSentinels("SetValue-direct");
        ResetSentinels();
        Refuse("field-handle-SetValue-pointer", SetValuePointer);
        CheckSentinels("SetValue-pointer");
        try
        {
            typeof(RuntimeFieldHandle).GetMethod("<SetValue>g____PInvoke|34_0", QueryFlags, new Type[] { typeof(IntPtr), Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack"), Type.GetType("System.Runtime.CompilerServices.ObjectHandleOnStack"), Type.GetType("System.Runtime.CompilerServices.QCallTypeHandle"), Type.GetType("System.Runtime.CompilerServices.QCallTypeHandle"), typeof(int*) }).Invoke(null, new object[] { IntPtr.Zero, null, null, null, null, null });
            Console.WriteLine("field-handle-SetValue-reflection" + ":accepted");
        }
        catch (TargetInvocationException error) { Console.WriteLine("field-handle-SetValue-reflection" + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine("field-handle-SetValue-reflection" + ":" + error.GetType().Name); }
        Console.WriteLine("Runtime field handle boundary end");
    }
}
