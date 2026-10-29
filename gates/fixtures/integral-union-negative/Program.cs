using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Explicit, Size = 8)]
internal struct Bad { [FieldOffset(0)] public int Field; [FieldOffset(0)] public uint Alias; [FieldOffset(4)] public int Tail; }
[StructLayout(LayoutKind.Explicit, Size = 4)]
internal struct InnerUnion { [FieldOffset(0)] public int Value; }
internal static class Program { private static void Main() => Console.WriteLine(Array.IndexOf(new Bad[1], default(Bad))); }
