namespace UnrealSharp.Interop
{
    public static unsafe class Bind_Utf8Fixture
    {
        public static bool Probe(nint address)
        {
            var callback = (delegate* unmanaged<string?, string?, int>)address;
            return callback(null, null) == 1
                && callback("alpha", "犬é😀") == 2
                && callback("犬é😀", "alpha") == 3;
        }
    }
}

namespace UnrealSharp.Core.Interop
{
    public static unsafe class Bind_Utf8Fixture
    {
        public static bool Probe(nint address)
        {
            var callback = (delegate* unmanaged<string?, string?, int>)address;
            return callback(null, null) == 1
                && callback("alpha", "犬é😀") == 2
                && callback("犬é😀", "alpha") == 3;
        }
    }
}

namespace UnrealSharp.Log
{
    public static unsafe class Bind_Utf8Fixture
    {
        public static bool Probe(nint address)
        {
            var callback = (delegate* unmanaged<string?, string?, int>)address;
            return callback(null, null) == 1
                && callback("alpha", "犬é😀") == 2
                && callback("犬é😀", "alpha") == 3;
        }
    }
}
