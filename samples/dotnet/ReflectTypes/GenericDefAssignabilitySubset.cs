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
        }
    }
}
