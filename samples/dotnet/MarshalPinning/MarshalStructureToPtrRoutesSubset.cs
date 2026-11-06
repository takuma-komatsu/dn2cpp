using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MarshalStructureToPtrRoutesSubset;

internal static unsafe class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Rec
    {
        public int A;
        public long B;
        public double C;
        public IntPtr D;
    }

    private static int s_checks;

    private static void Check(bool value, string label)
    {
        if (!value) throw new InvalidOperationException("StructureToPtr route: " + label);
        s_checks++;
    }

    private static bool Equal(Rec left, Rec right) =>
        left.A == right.A && left.B == right.B && left.C == right.C && left.D == right.D;

    private static void Write(int route, object value, IntPtr pointer, bool deleteOld, MethodInfo reflected)
    {
        switch (route)
        {
            case 0:
                Marshal.StructureToPtr(value, pointer, deleteOld);
                return;
            case 1:
                delegate* managed<object, IntPtr, bool, void> address = &Marshal.StructureToPtr;
                address(value, pointer, deleteOld);
                return;
            case 2:
                Action<object, IntPtr, bool> action = Marshal.StructureToPtr;
                action(value, pointer, deleteOld);
                return;
            case 3:
                reflected.Invoke(null, new object[] { value, pointer, deleteOld });
                return;
            default:
                throw new InvalidOperationException("Unknown StructureToPtr route");
        }
    }

    private static void NullArgument(int route, object value, IntPtr pointer, string parameter, MethodInfo reflected)
    {
        try
        {
            Write(route, value, pointer, false, reflected);
        }
        catch (ArgumentNullException error)
        {
            Check(route != 3 && error.ParamName == parameter, "direct-null-contract");
            return;
        }
        catch (TargetInvocationException error)
        {
            Check(route == 3 && error.InnerException is ArgumentNullException argument
                && argument.ParamName == parameter, "reflection-null-contract");
            return;
        }
        throw new InvalidOperationException("StructureToPtr accepted a null " + parameter);
    }

    internal static void Run()
    {
        Console.WriteLine("== Marshal StructureToPtr callable routes ==");
        s_checks = 0;
        MethodInfo reflected = typeof(Marshal).GetMethod(nameof(Marshal.StructureToPtr),
            new Type[] { typeof(object), typeof(IntPtr), typeof(bool) });
        Check(reflected != null && reflected.IsPublic && reflected.IsStatic
            && !reflected.IsGenericMethod && reflected.ReturnType == typeof(void), "exact-public-overload");
        // Prepare only this copyable layout. The object overload must still copy
        // from its runtime box rather than borrow the source or enter CoreCLR IL.
        int size = Marshal.SizeOf(typeof(Rec));
        Check(size == sizeof(Rec), "prepared-layout");
        string[] names = { "direct", "calli", "delegate", "reflection" };
        for (int route = 0; route < names.Length; route++)
        {
            Rec source = new Rec { A = 31 + route, B = 9000000000L + route,
                C = 1.25 + route, D = (IntPtr)(173 + route) };
            object boxed = source;
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    Write(route, boxed, buffer, pass != 0, reflected);
                    Check(Equal(*(Rec*)buffer, source), names[route] + "-copy");
                    ((Rec*)buffer)->A = -1;
                    ((Rec*)buffer)->B = -2;
                    ((Rec*)buffer)->C = -3.5;
                    ((Rec*)buffer)->D = (IntPtr)(-4);
                    Check(Equal((Rec)boxed, source), names[route] + "-source-independent");
                }
                NullArgument(route, null, buffer, "structure", reflected);
                NullArgument(route, boxed, IntPtr.Zero, "ptr", reflected);
                NullArgument(route, null, IntPtr.Zero, "ptr", reflected);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            Console.WriteLine("marshal-structure-to-ptr-route:" + names[route] + ":checks=7");
        }
        Console.WriteLine("marshal-structure-to-ptr-checks:" + s_checks);
        Console.WriteLine("Marshal StructureToPtr callable routes end");
    }
}
