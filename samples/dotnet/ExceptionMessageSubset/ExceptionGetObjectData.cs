#pragma warning disable SYSLIB0051 // GetObjectData override: the legacy serialization API is obsolete
#pragma warning disable CS0672    // overriding the obsolete Exception.GetObjectData is the point here
using System;
using System.Runtime.Serialization;

namespace ExceptionMessageSubset
{
    // System.Exception.GetObjectData(SerializationInfo, StreamingContext) — the
    // ISerializable base hook every serializable exception's override calls as
    // `base.GetObjectData(info, context)`. Exception is an intrinsic type, so
    // without a mapping for that base call ANY subclass carrying such an override
    // sinks the transpile merely by being reachable. The intercept is a
    // pop-and-no-op: formatter-based serialization is obsolete in modern .NET and
    // dn2cpp models no SerializationInfo writes, so nothing reads the payload back.
    //
    // C# member lookup skips override members, so every source-level
    // `ex.GetObjectData(...)` compiles to `callvirt System.Exception::GetObjectData`
    // whatever the receiver's static type, and the intercept therefore no-ops the
    // whole dispatch, derived overrides included. The diff stays exact because both
    // sides print the same reached-and-alive lines: real .NET dispatches and
    // ThrowIfNulls the null info (caught below), dn2cpp's no-op just returns, and
    // the exception objects are untouched either way.
    internal static class ExceptionGetObjectData
    {
        // A user exception whose override calls base.GetObjectData. Merely
        // transpiling this override is the assertion: the base call inside it must
        // map, or the build sinks.
        internal sealed class ProbeSerializableError : Exception
        {
            public ProbeSerializableError(string message) : base(message) { }
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
            }
        }

        internal static void Run()
        {
            // throw/catch of the override-carrying type: no call is needed to
            // provoke the transpile failure, a reachable override is enough.
            try
            {
                throw new ProbeSerializableError("god boom");
            }
            catch (ProbeSerializableError e)
            {
                Console.WriteLine("god-caught: " + e.Message);
            }

            // Direct call on the override-carrying type (compiles to the
            // Exception-declared callee, see the class header): reached at run
            // time, nothing crashes, the ArgumentNullException/no-op divergence is
            // swallowed, and the object reads back intact.
            var ex = new ProbeSerializableError("god direct");
            try
            {
                ex.GetObjectData(null!, default);
            }
            catch (ArgumentNullException)
            {
            }
            Console.WriteLine("god-called: reached message=" + ex.Message);

            // The same call shape over a BCL override (ArgumentException overrides
            // the base hook) through an Exception-typed receiver: no crash, and
            // ParamName/Message read back exact on both sides.
            Exception be = new ArgumentException("god arg", "p");
            try
            {
                be.GetObjectData(null!, default);
            }
            catch (ArgumentNullException)
            {
            }
            Console.WriteLine("god-bcl: " + ((ArgumentException)be).ParamName + " / " + be.Message);
        }
    }
}
