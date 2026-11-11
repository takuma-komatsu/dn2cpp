using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace VectorProbe;

internal struct VectorUnsupportedAbiWrapper<T> where T : struct
{
    public Vector<T> Value;
    public int Marker;
}

internal static partial class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T VectorUnsupportedAbiIdentity<T>(T value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void VectorUnsupportedAbiByRef<T>(ref T target, T value) { target = value; }

    // StorageSafety checks byte transport; these returned values must also retain
    // the correct supported/unsupported dispatch and exact boxed wrapper type.
    private static void VectorUnsupportedAbiResult<T>(string name, VectorUnsupportedAbiWrapper<T> value, Vector<T> expected) where T : struct
    {
        VectorUnsupportedRecord(name + ".marker", () => value.Marker == 23);
        VectorUnsupportedRecord(name + ".box-identity", () => ((object)value).GetType() == typeof(VectorUnsupportedAbiWrapper<T>));
        VectorUnsupportedRecord(name + ".typed", () => value.Value.Equals(expected));
    }

    private static void VectorUnsupportedAbiTest<T>(string name, Vector<T> value) where T : struct
    {
        var original = new VectorUnsupportedAbiWrapper<T> { Value = value, Marker = 23 };
        var direct = VectorUnsupportedAbiIdentity(original);
        VectorUnsupportedAbiResult(name + ".value", direct, value);
        VectorUnsupportedAbiWrapper<T> byref = default;
        VectorUnsupportedAbiByRef(ref byref, original);
        VectorUnsupportedAbiResult(name + ".byref", byref, value);
        Func<VectorUnsupportedAbiWrapper<T>, VectorUnsupportedAbiWrapper<T>> callable = VectorUnsupportedAbiIdentity;
        VectorUnsupportedAbiResult(name + ".delegate", callable(original), value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool VectorUnsupportedAbiSupported<T>() where T : struct => Vector<T>.IsSupported;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int VectorUnsupportedAbiCount<T>() where T : struct => Vector<T>.Count;

    private static void VectorUnsupportedInterfaceAddress<T>(string name) where T : struct
    {
        IEquatable<Vector<T>> receiver = default(Vector<T>);
        Func<Vector<T>, bool> address = receiver.Equals;
        VectorUnsupportedRecord(name, () => address(default));
    }

    private static void VectorUnsupportedAbiSemantics()
    {
        Console.WriteLine("-- vector unsupported ABI --");
        VectorUnsupportedAbiTest<VectorUnsupportedEnum>("abi.enum", default);
        VectorUnsupportedAbiTest<int>("abi.int", new Vector<int>(37));
        VectorUnsupportedRecord("abi.body-enum-supported", () => VectorUnsupportedAbiSupported<VectorUnsupportedEnum>());
        VectorUnsupportedRecord("abi.body-enum-count", () => VectorUnsupportedAbiCount<VectorUnsupportedEnum>() > 0);
        VectorUnsupportedRecord("abi.body-int-supported", () => VectorUnsupportedAbiSupported<int>());
        VectorUnsupportedRecord("abi.body-int-count", () => VectorUnsupportedAbiCount<int>() > 0);
        VectorUnsupportedInterfaceAddress<VectorUnsupportedEnum>("enum.interface-address");
        Console.WriteLine("vector unsupported ABI end");
    }
}
