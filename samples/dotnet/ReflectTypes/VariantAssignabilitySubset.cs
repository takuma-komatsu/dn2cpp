using System;
using System.Collections.Generic;

// Type.IsAssignableFrom over GENERIC VARIANCE, where the
// SOURCE is itself the variant closed generic rather than something that
// implements one.
//
// dn2cpp_type_is_assignable_from already delegates to dn2cpp_typeinfo_assignable
// — the one walk isinst runs — so the array arms and the class-implements-a-
// variant-interface arm answered right. What the walk omitted was the REFLEXIVE
// case: its variance fallback tried every interface ROW of the source's base
// chain and never the source type-info itself. Through isinst that omission is
// nearly invisible (an object's header type is a concrete class, so the variant
// instantiation is always a row), which is why it survived; through
// IsAssignableFrom, whose source is an arbitrary Type, it made
// typeof(IEnumerable<object>).IsAssignableFrom(typeof(IEnumerable<string>))
// read False while `(object)new List<string>() is IEnumerable<object>` read True
// — one question, two answers, which is precisely what delegating to one walk
// was supposed to make impossible.
//
// It was live for isinst too, and the delegate lines below are that half:
// variance is legal on DELEGATES, whose instances DO carry a variant closed
// generic as their header type, so `(object)(Func<string>)f is Func<object>`
// answered False as well. Measured, both before and after.
//
// Every line here matches real .NET (captured against `dotnet run` before the
// section was folded into this freeze bucket).
namespace VariantAssignabilitySubset
{
    internal class Animal { }

    internal sealed class Cat : Animal { }

    internal static class Program
    {
        internal static void Run()
        {
            Console.WriteLine("== variant assignability ==");

            // The source Type IS the variant interface — the reflexive arm.
            Console.WriteLine("itf->itf co  : " + typeof(IEnumerable<object>).IsAssignableFrom(typeof(IEnumerable<string>)));
            Console.WriteLine("itf->itf co2 : " + typeof(IEnumerable<Animal>).IsAssignableFrom(typeof(IEnumerable<Cat>)));
            Console.WriteLine("itf->itf ctr : " + typeof(IComparer<Cat>).IsAssignableFrom(typeof(IComparer<Animal>)));
            // ...and the negative it must not swallow: covariance runs one way only.
            Console.WriteLine("itf->itf neg : " + typeof(IEnumerable<string>).IsAssignableFrom(typeof(IEnumerable<object>)));

            // The source is a CLASS implementing the variant interface — the row
            // arm, which already answered before the fix. Kept as the contrast.
            Console.WriteLine("cls->itf co  : " + typeof(IEnumerable<object>).IsAssignableFrom(typeof(List<string>)));
            Console.WriteLine("cls->itf co2 : " + typeof(IEnumerable<Animal>).IsAssignableFrom(typeof(List<Cat>)));

            // The isinst side of the same question, which was always right.
            object l = new List<string>();
            Console.WriteLine("isinst cls   : " + (l is IEnumerable<object>));
            IEnumerable<Cat> cats = new List<Cat>();
            Console.WriteLine("isinst cls2  : " + (cats is IEnumerable<Animal>));

            // IsAssignableTo, the argument-swapped spelling of the same rule.
            Console.WriteLine("assignTo     : " + typeof(IEnumerable<string>).IsAssignableTo(typeof(IEnumerable<object>)));

            // Delegate variance: the instance's header type IS the variant closed
            // generic, so this reaches the reflexive arm through isinst.
            Func<string> fs = () => "x";
            object fo = fs;
            Console.WriteLine("dlg isinst   : " + (fo is Func<object>));
            Console.WriteLine("dlg assign   : " + typeof(Func<object>).IsAssignableFrom(typeof(Func<string>)));
            Action<object> ao = _ => { };
            object aoo = ao;
            Console.WriteLine("dlg contra   : " + (aoo is Action<string>));

            // Exact identity is unchanged by any of it.
            Console.WriteLine("itf->itf same: " + typeof(IEnumerable<string>).IsAssignableFrom(typeof(IEnumerable<string>)));
        }
    }
}
