#nullable enable
using System;

namespace ReflectTypeSubset
{
    interface IAnimal { }

    abstract class Animal : IAnimal { }

    sealed class Dog : Animal { }

    sealed class Cat : Animal { }

    enum Color { Red, Green, Blue }

    static class Program
    {
        static string BaseName(Type t)
        {
            Type? b = t.BaseType;
            return b is null ? "null" : b.Name;
        }internal static void Run()
        {
            // FullName / Namespace / Name — namespace-qualified vs simple, user type
            // and a primitive (both flow through the Dn2CppTypeInfo name).
            Console.WriteLine("== names ==");
            Console.WriteLine(typeof(Dog).FullName);     // ReflectTypeSubset.Dog
            Console.WriteLine(typeof(Dog).Namespace);    // ReflectTypeSubset
            Console.WriteLine(typeof(Dog).Name);         // Dog
            Console.WriteLine(typeof(int).FullName);     // System.Int32
            Console.WriteLine(typeof(int).Namespace);    // System

            // IsEnum / IsInterface / IsAbstract — flag-bit reads. In .NET an
            // interface is also abstract.
            Console.WriteLine("== enum/interface/abstract ==");
            Console.WriteLine(typeof(Color).IsEnum);     // True
            Console.WriteLine(typeof(Dog).IsEnum);       // False
            Console.WriteLine(typeof(IAnimal).IsInterface); // True
            Console.WriteLine(typeof(Dog).IsInterface);  // False
            Console.WriteLine(typeof(Animal).IsAbstract); // True
            Console.WriteLine(typeof(Dog).IsAbstract);   // False
            Console.WriteLine(typeof(IAnimal).IsAbstract); // True

            // IsArray — via a runtime GetType() on real arrays (value- and ref-element)
            // and a false on a plain class.
            Console.WriteLine("== array ==");
            Console.WriteLine(new int[1].GetType().IsArray);    // True
            Console.WriteLine(new string[1].GetType().IsArray); // True
            Console.WriteLine(typeof(Dog).IsArray);             // False

            // BaseType — chain walk + the reference-type -> Object synthesis at the
            // root, an enum's Enum base, and the null cases (object, interface).
            Console.WriteLine("== baseType ==");
            Console.WriteLine(BaseName(typeof(Dog)));    // Animal
            Console.WriteLine(BaseName(typeof(Animal))); // Object
            Console.WriteLine(BaseName(typeof(object))); // null
            Console.WriteLine(BaseName(typeof(IAnimal))); // null
            Console.WriteLine(BaseName(typeof(Color)));  // Enum

            // IsAssignableFrom — derivation, interface impl, object-accepts-all, self.
            Console.WriteLine("== assignable ==");
            Console.WriteLine(typeof(Animal).IsAssignableFrom(typeof(Dog)));  // True
            Console.WriteLine(typeof(Dog).IsAssignableFrom(typeof(Animal)));  // False
            Console.WriteLine(typeof(IAnimal).IsAssignableFrom(typeof(Dog))); // True
            Console.WriteLine(typeof(object).IsAssignableFrom(typeof(Dog)));  // True
            Console.WriteLine(typeof(Dog).IsAssignableFrom(typeof(Dog)));     // True

            // IsInstanceOfType — on a live instance.
            Console.WriteLine("== instanceOf ==");
            Dog dog = new Dog();
            Console.WriteLine(typeof(Animal).IsInstanceOfType(dog));  // True
            Console.WriteLine(typeof(IAnimal).IsInstanceOfType(dog)); // True
            Console.WriteLine(typeof(Cat).IsInstanceOfType(dog));     // False
            Console.WriteLine(typeof(object).IsInstanceOfType(dog));  // True

            // The runtime GetType() path threads the same Dn2CppTypeInfo as typeof.
            Console.WriteLine("== getType ==");
            Console.WriteLine(dog.GetType().Name);          // Dog
            Console.WriteLine(dog.GetType().BaseType!.Name); // Animal
            Console.WriteLine(dog.GetType().IsEnum);        // False
        }
    }
}
