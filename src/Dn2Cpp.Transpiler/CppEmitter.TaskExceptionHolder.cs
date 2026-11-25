namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private void EmitTaskSchedulerQueue(CppOutput output)
    {
        if (_c.TaskSchedulerQueue is not { } queue) return;
        var ctor = queue.ExceptionConstructor;
        var exception = ctor.DeclaringClass;
        output.Header.AppendLine("extern const Dn2CppTaskSchedulerQueue dn2cpp_image_task_scheduler_queue;");
        var text = output.Data;
        text.AppendLine("static Dn2CppObject* dn2cpp_image_wrap_scheduler_exception(Dn2CppObject* inner)");
        text.AppendLine("{");
        text.AppendLine($"    auto* error = dn2cpp_exception_new(&{exception.CppTypeInfoName}, nullptr, nullptr);");
        text.AppendLine($"    {ctor.Emittable.CppName}(({exception.CppStructName}*)error, ({CppTypes.Of(ctor.Signature.ParameterTypes[0])})inner);");
        text.AppendLine("    return error;");
        text.AppendLine("}");
        text.AppendLine($"extern const Dn2CppTaskSchedulerQueue dn2cpp_image_task_scheduler_queue = {{ {queue.Queue.VtableSlot}, {queue.Dequeue.VtableSlot}, &dn2cpp_image_wrap_scheduler_exception }};");
    }

    private void EmitTaskExceptionHolderHooks(CppOutput output)
    {
        if (_c.TaskExceptionHolderBridge is not { } bridge) return;
        var holder = bridge.Holder;
        string type = holder.CppStructName;
        string listType = bridge.Faults.Type.Class!.CppStructName;
        string ediType = bridge.Source.DeclaringClass.CppStructName;
        var text = output.Data;
        output.Header.AppendLine("Dn2CppObject* dn2cpp_image_task_holder_create(Dn2CppTask* task);");
        output.Header.AppendLine("void dn2cpp_image_task_holder_add(Dn2CppObject* holder, Dn2CppObject* value, int32_t cancellation);");
        output.Header.AppendLine("Dn2CppArrayRef* dn2cpp_image_task_holder_faults(Dn2CppObject* holder);");
        output.Header.AppendLine("Dn2CppObject* dn2cpp_image_task_holder_cancellation(Dn2CppObject* holder);");
        output.Header.AppendLine("void dn2cpp_image_task_holder_observe(Dn2CppObject* holder);");
        output.Header.AppendLine("void dn2cpp_image_task_holder_rethrow(Dn2CppObject* holder, int32_t cancellation);");
        text.AppendLine("Dn2CppObject* dn2cpp_image_task_holder_create(Dn2CppTask* task)");
        text.AppendLine("{");
        text.AppendLine($"    auto* holder = static_cast<{type}*>(dn2cpp_alloc(sizeof({type})));");
        text.AppendLine($"    ((Dn2CppObject*)holder)->type = &{holder.CppTypeInfoName};");
        if (Compilation.EffectiveFinalize(holder) is not { } finalizer || !_c.Reachable.Contains(finalizer))
            throw new InvalidOperationException("TaskExceptionHolder requires its managed finalizer");
        text.AppendLine("    dn2cpp_register_finalizer((Dn2CppObject*)holder);");
        text.AppendLine($"    {bridge.Constructor.Emittable.CppName}(holder, task);");
        text.AppendLine("    return (Dn2CppObject*)holder;");
        text.AppendLine("}");
        text.AppendLine("void dn2cpp_image_task_holder_add(Dn2CppObject* holder, Dn2CppObject* value, int32_t cancellation)");
        text.AppendLine($"{{ {bridge.Add.Emittable.CppName}(({type}*)holder, value, cancellation); }}");
        // The native owner holds the holder monitor. Reading the real storage does
        // not mark its EDI list observed, unlike GetExceptionDispatchInfos.
        text.AppendLine("Dn2CppArrayRef* dn2cpp_image_task_holder_faults(Dn2CppObject* value)");
        text.AppendLine("{");
        text.AppendLine($"    auto* list = ({listType}*)(({type}*)value)->{bridge.Faults.CppName};");
        text.AppendLine("    if (list == nullptr) return nullptr;");
        text.AppendLine($"    int32_t count = list->{bridge.Size.CppName};");
        text.AppendLine("    auto* result = dn2cpp_newarr_ref(count);");
        text.AppendLine($"    auto* items = (Dn2CppArrayRef*)list->{bridge.Items.CppName};");
        text.AppendLine("    for (int32_t i = 0; i < count; i++)");
        text.AppendLine("    {");
        text.AppendLine($"        auto* edi = ({ediType}*)items->data[i];");
        text.AppendLine($"        dn2cpp_gc_store_ref(&result->data[i], edi == nullptr ? nullptr : (Dn2CppObject*)edi->{bridge.Source.CppName});");
        text.AppendLine("    }");
        text.AppendLine("    return result;");
        text.AppendLine("}");
        text.AppendLine("Dn2CppObject* dn2cpp_image_task_holder_cancellation(Dn2CppObject* value)");
        text.AppendLine("{");
        text.AppendLine($"    auto* edi = ({ediType}*)(({type}*)value)->{bridge.Cancellation.CppName};");
        text.AppendLine($"    return edi == nullptr ? nullptr : (Dn2CppObject*)edi->{bridge.Source.CppName};");
        text.AppendLine("}");
        output.Header.AppendLine("Dn2CppArrayRef* dn2cpp_image_task_holder_dispatch_faults(Dn2CppObject* holder);");
        output.Header.AppendLine("Dn2CppObject* dn2cpp_image_task_holder_dispatch_cancellation(Dn2CppObject* holder);");
        text.AppendLine("Dn2CppArrayRef* dn2cpp_image_task_holder_dispatch_faults(Dn2CppObject* value)");
        text.AppendLine("{");
        text.AppendLine($"    auto* list = ({listType}*)(({type}*)value)->{bridge.Faults.CppName};");
        text.AppendLine("    if (list == nullptr) return nullptr;");
        text.AppendLine($"    int32_t count = list->{bridge.Size.CppName};");
        text.AppendLine("    auto* result = dn2cpp_newarr_ref(count);");
        text.AppendLine($"    auto* items = (Dn2CppArrayRef*)list->{bridge.Items.CppName};");
        text.AppendLine("    for (int32_t i = 0; i < count; i++)");
        text.AppendLine("        dn2cpp_gc_store_ref(&result->data[i], items->data[i]);");
        text.AppendLine("    return result;");
        text.AppendLine("}");
        text.AppendLine("Dn2CppObject* dn2cpp_image_task_holder_dispatch_cancellation(Dn2CppObject* value)");
        text.AppendLine($"{{ return (Dn2CppObject*)(({type}*)value)->{bridge.Cancellation.CppName}; }}");
        output.Header.AppendLine("Dn2CppArrayRef* dn2cpp_image_task_holder_normalize(Dn2CppArrayRef* source);");
        text.AppendLine("Dn2CppArrayRef* dn2cpp_image_task_holder_normalize(Dn2CppArrayRef* source)");
        text.AppendLine("{");
        text.AppendLine($"    auto* result = dn2cpp_newarr_ref_t(source->length, {MethodCompiler.PreciseArrayTypeInfoExprOf(bridge.Items.Type.Element!)});");
        text.AppendLine("    for (int32_t i = 0; i < source->length; i++)");
        text.AppendLine("    {");
        text.AppendLine("        auto* item = source->data[i];");
        text.AppendLine($"        if (item != nullptr && dn2cpp_isinst(item, &{bridge.Source.DeclaringClass.CppTypeInfoName}) == nullptr)");
        text.AppendLine($"            item = (Dn2CppObject*){bridge.Capture.Emittable.CppName}(({CppTypes.Of(bridge.Source.Type)})dn2cpp_castclass(item, &{bridge.Source.Type.Class!.CppTypeInfoName}));");
        text.AppendLine("        dn2cpp_gc_store_ref(&result->data[i], item);");
        text.AppendLine("    }");
        text.AppendLine("    return result;");
        text.AppendLine("}");
        text.AppendLine("void dn2cpp_image_task_holder_observe(Dn2CppObject* holder)");
        text.AppendLine($"{{ {bridge.Observe.Emittable.CppName}(({type}*)holder, 0); }}");
        text.AppendLine("void dn2cpp_image_task_holder_rethrow(Dn2CppObject* value, int32_t cancellation)");
        text.AppendLine("{");
        text.AppendLine($"    auto* holder = ({type}*)value;");
        text.AppendLine($"    {ediType}* edi;");
        text.AppendLine("    if (cancellation)");
        text.AppendLine("    {");
        text.AppendLine($"        edi = ({ediType}*)holder->{bridge.Cancellation.CppName};");
        text.AppendLine("        if (edi == nullptr) return;");
        text.AppendLine("    }");
        text.AppendLine("    else");
        text.AppendLine("    {");
        text.AppendLine($"        auto* list = ({listType}*)holder->{bridge.Faults.CppName};");
        text.AppendLine($"        if (list == nullptr || list->{bridge.Size.CppName} == 0) return;");
        text.AppendLine($"        edi = ({ediType}*)((Dn2CppArrayRef*)list->{bridge.Items.CppName})->data[0];");
        text.AppendLine("    }");
        text.AppendLine($"    {bridge.Rethrow.Emittable.CppName}(({ediType}*)dn2cpp_null_check((Dn2CppObject*)edi));");
        text.AppendLine("}");
    }
}
