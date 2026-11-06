internal static class VectorUnsupportedFamily
{
    internal static int Width { get; set; }
    internal static string Namespace => Width == 0 ? "System.Numerics" : "System.Runtime.Intrinsics";
    internal static string TypeName => Width == 0 ? "Vector`1" : "Vector" + Width + "`1";
    internal static string OwnerName => Namespace + "." + TypeName;
    internal static string HelperName => Width == 0 ? "ThrowForUnsupportedNumericsVectorBaseType"
        : Width == 64 ? "ThrowForUnsupportedIntrinsicsVector64BaseType" : "ThrowForUnsupportedIntrinsicsVector128BaseType";
}
