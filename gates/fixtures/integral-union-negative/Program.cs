using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Explicit, Size = 8)]
internal struct Bad { [FieldOffset(0)] public float Field; [FieldOffset(0)] public uint Alias; [FieldOffset(4)] public int Tail; }
internal static class Program { private static void Main() => Console.WriteLine(Array.IndexOf(new Bad[1], default(Bad))); }
