using System;

namespace ExceptionMessageSubset
{
    // The members an exception type declares ITSELF — ArgumentException.ParamName (a
    // virtual property), ArgumentOutOfRangeException.ActualValue, FileNotFoundException
    // .FileName — read off an exception the program constructed with `new`.
    //
    // Every one of them used to be a silent miscompile, and ParamName was a SIGSEGV:
    // the exception newobj was intercepted to the runtime's uniform allocator, which
    // stamped the type-info handle the RUNTIME owns for these BCL types. That handle
    // knew a name and a base and nothing else — no vtable (so `callvirt get_ParamName`
    // loaded slot 13 off a null table and jumped to 0x68), no instance size (so the
    // object had no storage for _paramName even if something had written it) and no
    // reflection tables — and the real ctor, the one that writes _paramName, was
    // skipped as having "nothing to run". The type-info now carries the emitted vtable,
    // size and tables (dn2cpp_type_binds, applied by dn2cpp_runtime_init) and the ctor
    // runs, so the object is complete whichever side allocated it — and it stays ONE
    // type-info, which is what keeps `catch`/`is`/`typeof` agreeing about it.
    //
    // .Message on the Argument*/FileNotFound family is exact now: the real ctor
    // chain runs so the base Exception::.ctor lands the BCL-computed message (a real
    // resource string recovered from the embedded .resources blob), and the used-virtual
    // reach pulls in each type's get_Message OVERRIDE — ArgumentException appending
    // "(Parameter 'x')", ArgumentOutOfRangeException adding "Actual value was N.",
    // FileNotFoundException building its text — dispatched even through a base-typed
    // (System.Exception) receiver. Those reads are exercised below.
    internal static class ExceptionVirtualMembers
    {
        // A user exception deriving from a BCL exception that the runtime also raises:
        // its own type-info chains to the runtime's handle for the base, so `catch
        // (ArgumentException)` still sees it — and its OVERRIDE of a base virtual is the
        // one that must dispatch.
        internal sealed class TaggedArgumentException : ArgumentException
        {
            private readonly string _tag;
            public TaggedArgumentException(string message, string paramName, string tag)
                : base(message, paramName) => _tag = tag;
            public override string ParamName => base.ParamName + "/" + _tag;
        }

        private static void P(string label, object v) => Console.WriteLine("vm " + label + ": " + (v ?? "<null>"));

        internal static void Run()
        {
            // ArgumentNullException(string paramName) — the crash case. ParamName is a
            // virtual property DECLARED ON THE BASE (ArgumentException), so the read is a
            // callvirt through a vtable slot the receiver's type-info has to carry.
            var ane = new ArgumentNullException("p");
            P("ane.ParamName", ane.ParamName);
            // .Message: base "Value cannot be null." + the get_Message override's
            // "(Parameter 'p')". The (c) route too — a base-typed receiver whose callee
            // resolves to System.Exception::get_Message still dispatches the override.
            P("ane.Message", ane.Message);
            P("ane via (Exception).Message", ((Exception)ane).Message);
            P("ane.InnerException", ane.InnerException);
            P("ane.GetType().Name", ane.GetType().Name);
            P("ane.GetType().FullName", ane.GetType().FullName);
            // One type-info per type: the handle typeof() names is the handle the object
            // carries. Two (an emitted ti_ beside the runtime's) and this is false.
            P("ane.GetType() == typeof", ane.GetType() == typeof(ArgumentNullException));
            P("ane is ArgumentException", ane is ArgumentException);
            P("ane is SystemException", ane is SystemException);
            P("ane is Exception", ane is Exception);

            // ArgumentException's own ctor shapes: (message) leaves ParamName null,
            // (message, paramName) sets it. Both messages are exact.
            var ae1 = new ArgumentException("just a message");
            P("ae1.ParamName", ae1.ParamName);
            P("ae1.Message", ae1.Message);
            var ae2 = new ArgumentException("m", "pn");
            P("ae2.ParamName", ae2.ParamName);
            // (message, paramName) shape: Message is "m (Parameter 'pn')".
            P("ae2.Message", ae2.Message);

            // ArgumentOutOfRangeException.ActualValue: an object-typed field the ctor
            // boxes into — a second declared field, past _paramName, on an allocation
            // that used to be the bare message/inner prefix.
            var aoore = new ArgumentOutOfRangeException("idx", 42, "out of range");
            P("aoore.ParamName", aoore.ParamName);
            // Message chains the base ArgumentException override ("out of range (Parameter
            // 'idx')") and appends its own "Actual value was 42." on a second line.
            P("aoore.Message", aoore.Message);
            P("aoore.ActualValue", aoore.ActualValue);
            P("aoore.ActualValue.GetType().Name", aoore.ActualValue.GetType().Name);
            P("aoore is ArgumentException", aoore is ArgumentException);
            var aooreNoValue = new ArgumentOutOfRangeException("idx2");
            P("aooreNoValue.ParamName", aooreNoValue.ParamName);
            P("aooreNoValue.ActualValue", aooreNoValue.ActualValue);

            // FileNotFoundException.FileName: same shape, a different BCL family.
            var fnf = new System.IO.FileNotFoundException("not found", "f.txt");
            P("fnf.FileName", fnf.FileName);
            // FileNotFoundException.get_Message dispatches its own override (which reads
            // the base _message field): with a message set, it returns it verbatim.
            P("fnf.Message", fnf.Message);
            P("fnf is IOException", fnf is System.IO.IOException);

            // InnerException chains through the same objects.
            var outer = new InvalidOperationException("outer", new FormatException("inner"));
            P("outer.Message", outer.Message);
            P("outer.InnerException.Message", outer.InnerException.Message);
            P("outer.InnerException.GetType().Name", outer.InnerException.GetType().Name);
            P("outer.InnerException.InnerException", outer.InnerException.InnerException);

            // Thrown and caught: the same object, reached through each of its base types.
            try
            {
                throw new ArgumentNullException("thrown");
            }
            catch (ArgumentException e)
            {
                P("caught as ArgumentException, ParamName", e.ParamName);
                P("caught, GetType().Name", e.GetType().Name);
            }
            try
            {
                throw new ArgumentOutOfRangeException("ti", 7, "why");
            }
            catch (Exception e)
            {
                P("caught as Exception, GetType().Name", e.GetType().Name);
                P("caught as Exception, downcast ActualValue", ((ArgumentOutOfRangeException)e).ActualValue);
                // The (c) route: `e` is statically System.Exception, so `e.Message`
                // resolves to the intrinsic base getter, which must still dispatch the
                // ArgumentOutOfRangeException override through the vtable.
                P("caught as Exception, Message", e.Message);
            }
            try
            {
                throw new ArgumentException("sys", "sp");
            }
            catch (SystemException e)
            {
                P("caught as SystemException, ParamName", ((ArgumentException)e).ParamName);
            }

            // A user override of a BCL exception's virtual, dispatched through the base
            // declaration's slot — and through a base-typed receiver.
            var tagged = new TaggedArgumentException("m", "up", "tag");
            P("tagged.ParamName", tagged.ParamName);
            ArgumentException asBase = tagged;
            P("tagged via base.ParamName", asBase.ParamName);
            try
            {
                throw tagged;
            }
            catch (ArgumentException e)
            {
                P("tagged caught as ArgumentException, ParamName", e.ParamName);
            }
        }
    }
}
