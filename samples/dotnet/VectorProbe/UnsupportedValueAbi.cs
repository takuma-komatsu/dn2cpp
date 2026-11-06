using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace VectorProbe;

internal struct VectorUnsupportedAbiWrapper<T> where T : struct
{
    public Vector<T> Value;
    public int Marker;
}
internal struct VectorUnsupportedAbiScalar<T> { public T Value; }

internal static partial class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T VectorUnsupportedAbiIdentity<T>(T value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void VectorUnsupportedAbiByRef<T>(ref T target, T value) { target = value; }

    private static void VectorUnsupportedAbiResult<T>(string name, VectorUnsupportedAbiWrapper<T> value) where T : struct
    {
        VectorUnsupportedRecord(name + ".marker", () => value.Marker == 23);
        VectorUnsupportedRecord(name + ".box-identity", () => ((object)value).GetType() == typeof(VectorUnsupportedAbiWrapper<T>));
        VectorUnsupportedStored(name, value.Value);
    }

    private static void VectorUnsupportedAbiTest<T>(string name) where T : struct
    {
        var original = new VectorUnsupportedAbiWrapper<T> { Marker = 23 };
        var direct = VectorUnsupportedAbiIdentity(original);
        VectorUnsupportedAbiResult(name + ".value", direct);
        VectorUnsupportedAbiWrapper<T> byref = default;
        VectorUnsupportedAbiByRef(ref byref, original);
        VectorUnsupportedAbiResult(name + ".byref", byref);
        Func<VectorUnsupportedAbiWrapper<T>, VectorUnsupportedAbiWrapper<T>> callable = VectorUnsupportedAbiIdentity;
        VectorUnsupportedAbiResult(name + ".delegate", callable(original));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool VectorUnsupportedAbiSupported<T>() where T : struct => Vector<T>.IsSupported;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int VectorUnsupportedAbiCount<T>() where T : struct => Vector<T>.Count;

    private static void VectorUnsupportedExtra<T>(string name) where T : struct
    {
        Vector<T> value = default;
        VectorUnsupportedRecord(name + ".Count", () => Vector<T>.Count);
        VectorUnsupportedRecord(name + ".typed", () => value.Equals(default(Vector<T>)));
        VectorUnsupportedRecord(name + ".object-null", () => value.Equals((object)null));
        VectorUnsupportedRecord(name + ".hash", () => value.GetHashCode());
    }

    private static void VectorUnsupportedInterfaceAddress<T>(string name) where T : struct
    {
        IEquatable<Vector<T>> receiver = default(Vector<T>);
        Func<Vector<T>, bool> address = receiver.Equals;
        VectorUnsupportedRecord(name, () => address(default));
    }

    private static void VectorUnsupportedAbiSemantics()
    {
        VectorUnsupportedAbiTest<VectorUnsupportedEnum>("abi.enum");
        VectorUnsupportedAbiTest<int>("abi.int");
        VectorUnsupportedAbiTest<uint>("abi.uint");
        var scalarEnum = VectorUnsupportedAbiIdentity(new VectorUnsupportedAbiScalar<VectorUnsupportedEnum> { Value = VectorUnsupportedEnum.One });
        var scalarInt = VectorUnsupportedAbiIdentity(new VectorUnsupportedAbiScalar<int> { Value = 7 });
        VectorUnsupportedRecord("abi.scalar-enum", () => scalarEnum.Value == VectorUnsupportedEnum.One);
        VectorUnsupportedRecord("abi.scalar-int", () => scalarInt.Value == 7);
        VectorUnsupportedRecord("abi.body-enum-supported", () => VectorUnsupportedAbiSupported<VectorUnsupportedEnum>());
        VectorUnsupportedRecord("abi.body-enum-count", () => VectorUnsupportedAbiCount<VectorUnsupportedEnum>() > 0);
        VectorUnsupportedRecord("abi.body-int-supported", () => VectorUnsupportedAbiSupported<int>());
        VectorUnsupportedRecord("abi.body-int-count", () => VectorUnsupportedAbiCount<int>() > 0);
        VectorUnsupportedExtra<Int128>("int128");
        VectorUnsupportedExtra<UInt128>("uint128");
        VectorUnsupportedInterfaceAddress<decimal>("decimal.interface-address");
        VectorUnsupportedInterfaceAddress<VectorUnsupportedEnum>("enum.interface-address");
        VectorUnsupportedInterfaceAddress<VectorUnsupportedStruct>("struct.interface-address");
        VectorUnsupportedInterfaceAddress<VectorUnsupportedRefStruct>("ref-containing-struct.interface-address");
    }
}
