#nullable enable
// SUBJECT: the Type predicates — IsPrimitive and its neighbours — read through
// BOTH arms that answer them: a `Type` passed as an ARGUMENT, which nothing can
// fold, and a bare typeof(X).IsPrimitive, which dn2cpp folds to a constant. The
// two must agree; the CLR's primitive set is 14 members, excluding Void, String,
// Object and TypedReference.
//
// `typeof(void)` is deliberately absent from the FOLDED list: real .NET answers
// that one cell two ways in one process — the folded read is TRUE (RyuJIT counts
// CORINFO_TYPE_VOID as primitive) while every reflection read of the same
// RuntimeType is FALSE. dn2cpp answers FALSE on both arms, so no live diff can
// assert the cell; the divergence is frozen in ReflectTypes/TypeCategorySubset.
using System;
using System.Reflection;

namespace TypePredicateFoldSubset;

static class Program
{
    private static void Probe() { }

    // The unfoldable arm: `t` arrives as an argument, so every read is the
    // runtime's own answer off the emitter-stamped DN2CPP_TF_* flags.
    private static void Show(string label, Type t)
        => Console.WriteLine($"{label}: primitive={t.IsPrimitive} valueType={t.IsValueType} "
            + $"class={t.IsClass} enum={t.IsEnum} name={t.Name}");

    internal static void Run()
    {
        Console.WriteLine("== Type predicates through a Type nothing can fold ==");
        // The exclusions from the CLR's 14 primitives, and representatives of each
        // side of the line.
        Show("void", typeof(void));
        Show("TypedReference", typeof(TypedReference));
        Show("string", typeof(string));
        Show("object", typeof(object));
        Show("int", typeof(int));
        Show("nint", typeof(IntPtr));
        Show("char", typeof(char));
        Show("bool", typeof(bool));
        Show("decimal", typeof(decimal));
        Show("DateTime", typeof(DateTime));
        Show("DayOfWeek", typeof(DayOfWeek));
        Show("int[]", typeof(int[]));

        // A void ReturnType off a MethodInfo, and the identity that makes the two
        // arms the same question: this Type IS typeof(void).
        Type ret = typeof(Program).GetMethod("Probe", BindingFlags.NonPublic | BindingFlags.Static)!.ReturnType;
        Show("Probe().ReturnType", ret);
        Console.WriteLine($"ReturnType is typeof(void): {ReferenceEquals(ret, typeof(void))}");

        Console.WriteLine("== the same predicates FOLDED at the call site ==");
        // typeof(void) is absent here on purpose — see the header. Every entry
        // below is one both runtimes answer identically through both arms.
        Console.WriteLine($"TypedReference={typeof(TypedReference).IsPrimitive} "
            + $"string={typeof(string).IsPrimitive} object={typeof(object).IsPrimitive} "
            + $"decimal={typeof(decimal).IsPrimitive} DayOfWeek={typeof(DayOfWeek).IsPrimitive} "
            + $"int[]={typeof(int[]).IsPrimitive}");
        Console.WriteLine($"int={typeof(int).IsPrimitive} uint={typeof(uint).IsPrimitive} "
            + $"long={typeof(long).IsPrimitive} ulong={typeof(ulong).IsPrimitive} "
            + $"short={typeof(short).IsPrimitive} ushort={typeof(ushort).IsPrimitive} "
            + $"byte={typeof(byte).IsPrimitive} sbyte={typeof(sbyte).IsPrimitive} "
            + $"char={typeof(char).IsPrimitive} bool={typeof(bool).IsPrimitive} "
            + $"float={typeof(float).IsPrimitive} double={typeof(double).IsPrimitive} "
            + $"nint={typeof(IntPtr).IsPrimitive} nuint={typeof(UIntPtr).IsPrimitive}");
        // Asserted as ONE boolean so a fold that admits an extra type is a single
        // changed line rather than a table to read.
        bool agree = true;
        foreach (Type t in new[] { typeof(TypedReference), typeof(string), typeof(object),
                                   typeof(decimal), typeof(DayOfWeek), typeof(int[]) })
            agree &= !t.IsPrimitive;
        foreach (Type t in new[] { typeof(int), typeof(uint), typeof(long), typeof(ulong),
                                   typeof(short), typeof(ushort), typeof(byte), typeof(sbyte),
                                   typeof(char), typeof(bool), typeof(float), typeof(double),
                                   typeof(IntPtr), typeof(UIntPtr) })
            agree &= t.IsPrimitive;
        Console.WriteLine($"folded == unfoldable for all 20: {agree}");
    }
}
