namespace XGenericMethodLib
{
    // Generic methods on a NON-generic type, so a cross-assembly call site
    // references them as a MethodSpec over a TypeReference-parented MemberRef.
    public static class Lib
    {
        public static T Echo<T>(T x) => x;

        public static T Pick<T>(bool takeFirst, T a, T b) => takeFirst ? a : b;

        public static int PairTag<T, U>(T a, U b) => 2;
    }
}
