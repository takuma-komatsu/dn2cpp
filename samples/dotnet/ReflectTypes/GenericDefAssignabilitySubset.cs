using System;
using System.Collections;
using System.Collections.Generic;

// Type.IsAssignableFrom where one END is a generic type DEFINITION —
// typeof(Box<>), never a closed type.
//
// A definition's base and interfaces are spelled in its own type parameters,
// which no caller can name, so .NET answers True only to the ARGUMENT-FREE
// relations and False to every relation with a generic end. The argument-free
// set is invariant under substitution, so it is the same for the definition and
// for every close of it — which is why the emitter can put it on the definition
// handle and why Box<> below answers with NO closed instantiation anywhere in
// the program. That is the case the registry-scanning predecessor could not
// answer: with nothing to scan it reported False where .NET reports True, and
// MessagePipe's AddGlobalMessageHandlerFilter(typeof(LoggingFilter<>)) is the
// same shape reached through a generic base (Leaf<>/IMarker2 below).
//
// Every line here matches real .NET.
namespace GenericDefAssignabilitySubset
{
    internal interface IMarker { }

    internal interface IMarker2 { }

    // Declared, reflected over, and never instantiated — closing it would give
    // the old registry scan something to find and hide the regression.
    internal class Box<T> : IMarker { }

    internal class NgBase { }

    internal class Der<T> : NgBase { }

    internal class Mid<T> : IMarker2 { }

    // Inherits IMarker2 through a GENERIC base, so the closure walk has to cross
    // one to find it; Leaf's own interface list is empty.
    internal class Leaf<T> : Mid<T> { }

    internal class NgRoot { }

    internal class ClosedBase<T> : NgRoot { }

    internal interface IClosedItf<T> { }

    // A CLOSED generic base/interface names no type parameter, so it is as
    // substitution-invariant as NgBase above and .NET keeps it on the definition:
    // typeof(Fixed<>).BaseType is ClosedBase<int>, not object.
    internal class Fixed<T> : ClosedBase<int>, IClosedItf<string> { }

    // Control: the same base spelled in the definition's own parameter, which no
    // caller can name — every fixed-argument relation on it stays False.
    internal class Dep<T> : ClosedBase<T> { }

    internal class SoloBase<T> : NgRoot { }

    internal interface ISoloItf<T> { }

    // Referenced ONLY as typeof(Solo<>) — no typeof of the closes, no
    // instantiation of anything here. The ancestry wiring itself must mint
    // SoloBase<int> / ISoloItf<string>, or BaseType degrades past them.
    internal class Solo<T> : SoloBase<int>, ISoloItf<string> { }

    internal class WideBase<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10,
        T11, T12, T13, T14, T15, T16, T17, T18, T19> : NgRoot { }

    // 19 arguments: the GENERICINST's argument count byte equals the VAR code
    // (0x13), so a byte-level invariance scan would mistake this closed base for
    // a parameter-mentioning one and drop it. Typeof-only, like Solo above.
    internal class Wide<T> : WideBase<int, int, int, int, int, int, int, int, int, int,
        int, int, int, int, int, int, int, int, int> { }

    internal static class Program
    {
        internal static void Run()
        {
            Console.WriteLine("== generic-definition assignability ==");

            // The argument-free relations, answered off the definition alone.
            Console.WriteLine("marker<-box  : " + typeof(IMarker).IsAssignableFrom(typeof(Box<>)));
            Console.WriteLine("ngbase<-der  : " + typeof(NgBase).IsAssignableFrom(typeof(Der<>)));
            Console.WriteLine("marker2<-leaf: " + typeof(IMarker2).IsAssignableFrom(typeof(Leaf<>)));
            Console.WriteLine("ienum<-list  : " + typeof(IEnumerable).IsAssignableFrom(typeof(List<>)));
            Console.WriteLine("object<-list : " + typeof(object).IsAssignableFrom(typeof(List<>)));

            // Anything with a generic end: False, definition or close.
            Console.WriteLine("ienumT<-listI: " + typeof(IEnumerable<>).IsAssignableFrom(typeof(List<int>)));
            Console.WriteLine("ienumT<-listT: " + typeof(IEnumerable<>).IsAssignableFrom(typeof(List<>)));
            Console.WriteLine("marker<-boxI : " + typeof(Box<>).IsAssignableFrom(typeof(IMarker)));

            // Identity still holds, and a definition matches no instance.
            Console.WriteLine("listT<-listT : " + typeof(List<>).IsAssignableFrom(typeof(List<>)));
            Console.WriteLine("listT inst   : " + typeof(List<>).IsInstanceOfType(new List<int>()));

            // The definition's own view of the same rows.
            Console.WriteLine("box<> itfs   : " + typeof(Box<>).GetInterfaces().Length);
            Console.WriteLine("der<> base   : " + typeof(Der<>).BaseType);

            // Fixed-argument (closed) relations: invariant under substitution, so
            // they too live on the definition. The typeof operands double as the
            // emitted closes the definition's rows point at.
            Console.WriteLine("fixed<> base : " + typeof(Fixed<>).BaseType);
            Console.WriteLine("cbi<-fixed   : " + typeof(ClosedBase<int>).IsAssignableFrom(typeof(Fixed<>)));
            Console.WriteLine("ics<-fixed   : " + typeof(IClosedItf<string>).IsAssignableFrom(typeof(Fixed<>)));
            Console.WriteLine("object<-fixed: " + typeof(object).IsAssignableFrom(typeof(Fixed<>)));
            Console.WriteLine("ngroot<-fixed: " + typeof(NgRoot).IsAssignableFrom(typeof(Fixed<>)));
            bool hasClosedItf = false;
            foreach (var itf in typeof(Fixed<>).GetInterfaces())
                if (itf == typeof(IClosedItf<string>))
                    hasClosedItf = true;
            Console.WriteLine("fixed<> itfs : " + hasClosedItf);
            Console.WriteLine("cbs<-fixed   : " + typeof(ClosedBase<string>).IsAssignableFrom(typeof(Fixed<>)));
            Console.WriteLine("icT<-fixed   : " + typeof(IClosedItf<>).IsAssignableFrom(typeof(Fixed<>)));
            Console.WriteLine("cbi<-dep     : " + typeof(ClosedBase<int>).IsAssignableFrom(typeof(Dep<>)));

            // The closes exist ONLY through these two reads.
            Console.WriteLine("solo<> base  : " + typeof(Solo<>).BaseType);
            foreach (var itf in typeof(Solo<>).GetInterfaces())
                Console.WriteLine("solo<> itf   : " + itf);
            Console.WriteLine("wide<> base  : " + typeof(Wide<>).BaseType);
            Console.WriteLine("ngroot<-wide : " + typeof(NgRoot).IsAssignableFrom(typeof(Wide<>)));
        }
    }
}
