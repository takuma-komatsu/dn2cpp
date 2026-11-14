using System;
using System.Globalization;
using System.Reflection;

namespace CustomAttributeConstructorSubset;

public static class ConstructorChecks
{
    public static int ConstructorCalls;
    [AttributeUsage(AttributeTargets.All,AllowMultiple=true)] public sealed class MarkAttribute:Attribute
    {
        public MarkAttribute(int value) { ConstructorCalls++; }
        public MarkAttribute(string value) { ConstructorCalls++; }
    }
    [Mark(3),Mark("x")] public class Tagged { }
    public class BaseData:CustomAttributeData { }
    public class OverrideData:CustomAttributeData
    {
        public override ConstructorInfo Constructor=>Chosen;
        public virtual int UnrelatedVirtual()=>91;
    }
    // This declaration is visible to planning, but no allocation or reflection root uses it.
    public class UnallocatedData:CustomAttributeData
    {
        public override ConstructorInfo Constructor=>throw new InvalidOperationException("Unallocated constructor override was rooted");
    }
    public class FurtherData:OverrideData { public override ConstructorInfo Constructor=>StringChosen; }
    static readonly ConstructorInfo StringChosen=typeof(MarkAttribute).GetConstructor(new[]{typeof(string)});
    static readonly ConstructorInfo Chosen=typeof(MarkAttribute).GetConstructor(new[]{typeof(int)});
    public delegate ConstructorInfo Bound();
    static void Culture() { CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture; }
    static void Check(string label,bool value) {Console.WriteLine("cad-constructor-"+label+":"+value);if(!value)throw new InvalidOperationException(label);}
    public static void Run()
    {
        Culture();Console.WriteLine("== CustomAttributeData constructor boundary ==");var rows=CustomAttributeData.GetCustomAttributes(typeof(Tagged));
        Check("data-types",rows.Count==2&&rows[0].AttributeType==typeof(MarkAttribute)&&rows[1].AttributeType==typeof(MarkAttribute));
        Check("data-no-construction",ConstructorCalls==0);Console.WriteLine("CustomAttributeData constructor boundary end");
    }
}
