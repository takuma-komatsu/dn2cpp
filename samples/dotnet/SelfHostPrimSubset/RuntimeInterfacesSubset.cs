using System;
using System.Globalization;
using System.Reflection;
namespace RuntimeInterfacesSubset;
internal static class Program
{
    public interface IA {} public interface IB:IA {} public interface IG<T> {}
    public class Base:IB {} public class Closed<T>:Base,IA,IG<T> {}
    public class Constrained<T> where T:IB {}
    private delegate Type[] Factory(Type type);
    internal static void Run()
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime handle interfaces ==");
        Console.WriteLine("interfaces-public-empty:"+(typeof(object).GetInterfaces().Length==0));
        Type[] parents=typeof(IB).GetInterfaces();
        Console.WriteLine("interfaces-public-parent:"+(parents.Length==1 && parents[0]==typeof(IA)));
        Console.WriteLine("Runtime handle interfaces end");
    }
}
