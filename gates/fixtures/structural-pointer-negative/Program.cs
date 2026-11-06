internal unsafe struct Bad { public byte* Field; public int Other; }
internal static class Program
{
    private static void Main() => Console.WriteLine(Array.IndexOf(new Bad[1], default(Bad)));
}
