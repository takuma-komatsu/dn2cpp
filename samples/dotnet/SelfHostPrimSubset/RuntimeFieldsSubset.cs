using System;
using System.Globalization;
using System.Reflection;
namespace RuntimeFieldsSubset;
internal static class Program
{
 public class Target { public int Value=17; public static int StaticValue=23; }
 public class Generic<T>{}
 private delegate bool Query(Type type,Span<nint> buffer,ref int count);
 internal static void Run(){CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;Console.WriteLine("== Runtime handle fields boundary ==");var target=new Target();Console.WriteLine("fields-public-count:"+(typeof(Target).GetFields(BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly).Length==2));Console.WriteLine("fields-public-value:"+((int)typeof(Target).GetField("Value").GetValue(target)==17));Console.WriteLine("Runtime handle fields boundary end");}
}
