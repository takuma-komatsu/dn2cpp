using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal sealed record TaskExceptionHolderMembers(ClassInfo Holder, MethodInfo Constructor,
        MethodInfo Add, MethodInfo Observe, MethodInfo Rethrow, MethodInfo Capture, FieldInfo Faults, FieldInfo Cancellation,
        FieldInfo Size, FieldInfo Items, FieldInfo Source);

    internal TaskExceptionHolderMembers? TaskExceptionHolderBridge { get; private set; }

    private void NoteTaskExceptionHolder(MethodInfo operation)
    {
        if (TaskExceptionHolderBridge is not null) return;
        var holder = FindClassByFullName("System.Threading.Tasks.TaskExceptionHolder");
        if (holder is null || holder.Module != operation.Module || holder.Handle.IsNil
            || holder.GenericArity != 0 || holder.IsValueType || holder.IsInterface)
            throw new NotSupportedException("Task.AddException requires its CoreLib TaskExceptionHolder");
        EnsureCompleted(holder);
        FieldInfo Field(ClassInfo owner, string name) => owner.Fields.FirstOrDefault(f => !f.IsStatic && f.Name == name)
            ?? throw new NotSupportedException("Task exception holder requires its managed field " + name);
        MethodInfo Method(string name, int arity) => holder.Methods.FirstOrDefault(m => !m.IsStatic
            && m.Name == name && m.Signature.ParameterTypes.Length == arity)
            ?? throw new NotSupportedException("Task exception holder requires its managed method " + name);
        bool ManagedInstance(MethodInfo method, bool constructor = false) =>
            method.Module == holder.Module && !method.Handle.IsNil && !method.IsSynthetic
            && method.Rva != 0 && method.PInvoke is null && method.ImplAttributes == MethodImplAttributes.IL
            && method.Context.MethodArgs.Length == 0
            && method.Module.Reader.GetMethodDefinition(method.Handle).GetGenericParameters().Count == 0
            && method.Signature.Header.RawValue == 0x20 && method.Signature.GenericParameterCount == 0
            && method.Signature.RequiredParameterCount == method.Signature.ParameterTypes.Length
            && method.Attributes == (MethodAttributes.Assembly | MethodAttributes.HideBySig
                | (constructor ? MethodAttributes.SpecialName | MethodAttributes.RTSpecialName : 0));
        var constructor = Method(".ctor", 1);
        var add = Method("Add", 2);
        var observe = Method("MarkAsHandled", 1);
        var faults = Field(holder, "m_faultExceptions");
        var cancellation = Field(holder, "m_cancellationException");
        if (faults.Type is not { Kind: TypeKind.Class, Class: { } list }
            || list.Module != holder.Module || GenericDefFullName(list) != "System.Collections.Generic.List"
            || list.Context.TypeArgs is not [{ Kind: TypeKind.Class, Class: { } edi }]
            || edi.Module != holder.Module || edi.FullName != "System.Runtime.ExceptionServices.ExceptionDispatchInfo"
            || cancellation.Type.Class != edi)
            throw new NotSupportedException("Task exception holder requires its managed EDI storage");
        EnsureCompleted(list);
        EnsureCompleted(edi);
        var size = Field(list, "_size");
        var items = Field(list, "_items");
        var source = Field(edi, "_exception");
        var rethrow = edi.Methods.FirstOrDefault(m => m.Name == "Throw" && !m.IsStatic
            && m.Signature.ParameterTypes.Length == 0 && m.Signature.ReturnType.IsVoid)
            ?? throw new NotSupportedException("Task exception holder requires EDI.Throw()");
        if (!ManagedInstance(constructor, constructor: true) || !ManagedInstance(add) || !ManagedInstance(observe)
            || rethrow.Module != holder.Module || rethrow.Handle.IsNil || rethrow.IsSynthetic
            || rethrow.Rva == 0 || rethrow.PInvoke is not null || rethrow.ImplAttributes != MethodImplAttributes.IL
            || rethrow.Context.MethodArgs.Length != 0 || rethrow.Signature.Header.RawValue != 0x20
            || rethrow.Signature.GenericParameterCount != 0
            || rethrow.Module.Reader.GetMethodDefinition(rethrow.Handle).GetGenericParameters().Count != 0
            || size.Type is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }
            || items.Type is not { Kind: TypeKind.SZArray, Element: { Class: { } element } } || element != edi
            || source.Type.Class is not { FullName: "System.Exception" } exception || exception.Module != holder.Module
            || !constructor.Signature.ReturnType.IsVoid || constructor.Signature.ParameterTypes[0].Class != operation.DeclaringClass
            || !add.Signature.ReturnType.IsVoid || add.Signature.ParameterTypes is not [
                { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object },
                { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }]
            || !observe.Signature.ReturnType.IsVoid || observe.Signature.ParameterTypes is not [
                { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }])
            throw new NotSupportedException("Task exception holder requires its exact managed storage and methods");
        var capture = edi.Methods.SingleOrDefault(m => m.Name == "Capture" && m.IsStatic
            && m.Module == holder.Module && !m.Handle.IsNil && m.Rva != 0
            && m.ImplAttributes == MethodImplAttributes.IL && m.Signature.Header.RawValue == 0
            && m.Signature.GenericParameterCount == 0 && m.Signature.ReturnType.Class == edi
            && m.Signature.ParameterTypes is [{ Class: { } parameter }] && parameter == exception)
            ?? throw new NotSupportedException("Task exception holder requires EDI.Capture(Exception)");
        TaskExceptionHolderBridge = new(holder, constructor, add, observe, rethrow, capture, faults, cancellation, size, items, source);
        NoteArrayElementType(TypeDesc.MakeClass(edi));
        Reach(capture);
        ReachAllocatedType(holder);
        NoteReferencedType(list);
        NoteReferencedType(edi);
        Reach(constructor);
        Reach(add);
        Reach(observe);
        Reach(rethrow);
    }
}
