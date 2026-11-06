using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace VectorProbe;

internal static partial class Program
{
    private enum StorageEnum { Zero }
    private enum OtherStorageEnum { Zero }
    private struct StorageWrapper<T> where T : struct
    {
        public Vector<T> Value;
        public int Marker;
    }
    private struct FixedStorageWrapper<T> where T : struct
    {
        public Vector128<T> Value;
        public int Marker;
    }
    private interface IStorageMutation
    {
        int Change();
    }
    private struct BoxedStorage : IStorageMutation
    {
        public byte Prefix;
        public Vector128<int> Value;
        public int Marker;
        public string Tag;

        // Keep the storage fixture independent of structural vector equality.
        public override bool Equals(object other) => false;
        public override int GetHashCode() => 0;

        public int Change()
        {
            Value = Vector128.Create(19);
            Marker++;
            Tag = "retained";
            return Prefix + Value.GetElement(0) + Marker + Tag.Length;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T StorageUnbox<T>(object value) => (T)value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T StorageIdentity<T>(T value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StorageCopy<T>(ref T target, T value) => target = value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int StorageIndirect()
    {
        byte* bytes = stackalloc byte[96];
        ref Vector128<int> slot = ref Unsafe.AsRef<Vector128<int>>(bytes + 1);
        slot = Vector128.Create(43);
        Vector128<int> copied = slot;
        slot = default;
        return copied.GetElement(0) + slot.GetElement(0);
    }

    private static void VectorStorageSafety()
    {
        Console.WriteLine("-- vector storage safety --");
        var vector = new Vector<int>(37);
        Console.WriteLine("unbox=" + StorageUnbox<Vector<int>>(vector)[0]);
        Vector<int>? nullable = StorageUnbox<Vector<int>?>(vector);
        Console.WriteLine("nullable=" + nullable.Value[0] + "," + StorageUnbox<Vector<int>?>(null).HasValue);
        Console.WriteLine("scalar=" + StorageUnbox<int>(37) + "," + StorageUnbox<long?>((object)41L).Value);
        bool rejected = false;
        try { StorageUnbox<Vector<int>>(new Vector<byte>(1)); }
        catch (InvalidCastException) { rejected = true; }
        Console.WriteLine("wrong type=" + rejected);
        bool nullRejected = false;
        try { StorageUnbox<Vector<int>>(null); }
        catch (NullReferenceException) { nullRejected = true; }
        Console.WriteLine("null type=" + nullRejected);

        var managed = StorageIdentity(new StorageWrapper<StorageEnum> { Value = default, Marker = 27 });
        var other = new StorageWrapper<OtherStorageEnum>();
        StorageCopy(ref other, StorageIdentity(new StorageWrapper<OtherStorageEnum> { Value = default, Marker = 29 }));
        Console.WriteLine("managed storage=" + Unsafe.SizeOf<StorageWrapper<StorageEnum>>() + "," + managed.Marker + "," + other.Marker);
        var fixedManaged = StorageIdentity(new FixedStorageWrapper<StorageEnum> { Value = default, Marker = 31 });
        Console.WriteLine("fixed managed marker=" + fixedManaged.Marker);
        IStorageMutation box = new BoxedStorage { Prefix = 2, Value = Vector128.Create(3), Marker = 5, Tag = "original" };
        Console.WriteLine("boxed mutation=" + box.Change() + "," + box.Change());
        Console.WriteLine("indirect=" + StorageIndirect());
        Console.WriteLine("vector storage safety end");
    }
}
