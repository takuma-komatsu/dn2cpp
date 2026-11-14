using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeCacheGenericDefinitionSubset;

public static class DefinitionChecks
{
    public class G<T> { public class N<U> { } }
    public delegate Type Getter();
    delegate Type Route(object cache);
    static Type[] Inputs()=>new[]{typeof(G<>),typeof(G<int>),typeof(G<string>),typeof(G<int>.N<string>),typeof(G<>.N<>),typeof(Dictionary<int,string>),typeof(List<int>),typeof(int?)};
    static Type[] Definitions()=>new[]{typeof(G<>),typeof(G<>),typeof(G<>),typeof(G<>.N<>),typeof(G<>.N<>),typeof(Dictionary<,>),typeof(List<>),typeof(Nullable<>)};
    static void Culture(){CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;}
    static void Check(string label,bool value){Console.WriteLine("cache-generic-definition-"+label+":"+value);if(!value)throw new InvalidOperationException(label);}
    public static void Run()
    {
        Culture();Console.WriteLine("== RuntimeType cache generic definition ==");Type[] input=Inputs(),expected=Definitions();
        for(int i=0;i<input.Length;i++)Check("public-"+i,input[i].GetGenericTypeDefinition()==expected[i]);
        Check("public-same-definition",typeof(G<int>).GetGenericTypeDefinition()==typeof(G<string>).GetGenericTypeDefinition());
        Console.WriteLine("RuntimeType cache generic definition end");
    }
}
