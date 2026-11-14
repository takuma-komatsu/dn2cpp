using System;
using System.Reflection;

namespace CollectibilitySubset
{
    public class Owner<T>
    {
        public T Field = default!;
        public T Property { get; set; } = default!;
        public T Echo(T value) => value;
    }

    public static class Program
    {
        private static void Check(string label, bool value)
        {
            Console.WriteLine("collectibility-" + label + ":" + value);
            if (!value) throw new InvalidOperationException(label);
        }

        public static void Run()
        {
            Console.WriteLine("== noncollectible reflection types ==");
            Type[] types = { typeof(int), typeof(Owner<>), typeof(Owner<int>), typeof(int[]),
                typeof(int).MakeByRefType(), typeof(int).MakePointerType(), typeof(Owner<>).GetGenericArguments()[0] };
            for (int i = 0; i < types.Length; i++)
                Check("type-" + i, !types[i].IsCollectible && !((MemberInfo)types[i]).IsCollectible);
            foreach (MemberInfo member in typeof(Owner<int>).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                Check("member-" + member.Name, !member.IsCollectible);
            Console.WriteLine("noncollectible reflection types end");
        }
    }
}
