#include <cstring>
#include <iostream>
#include <cassert>

#include "vroomjs.h"
#include "JsEngine.h"
#include "ClrObjectRef.h"
#include "JsContext.h"


long js_mem_debug_engine_count;

static const int Mega = 1024 * 1024;


JsEngine::JsEngine(int32_t max_young_space, int32_t max_old_space, jscallbacks callbacks)
    :callbacks_(callbacks)
{
    allocator_ = v8::ArrayBuffer::Allocator::NewDefaultAllocator();

    Isolate::CreateParams create_params;
    create_params.array_buffer_allocator = allocator_;

    isolate_ = Isolate::New(create_params);

    // todo: is there a perf penalty for this? maybe it should be an option on the engine
    isolate_->SetCaptureStackTraceForUncaughtExceptions(true);

    // todo: SetResourceConstraints doesn't seem to exist anymore
    //isolate_->Enter();
    //if (max_young_space > 0 && max_old_space > 0) {
    //	v8::ResourceConstraints constraints;
    //	constraints.set_max_young_space_size(max_young_space * Mega);
    //	constraints.set_max_old_space_size(max_old_space * Mega);
    //
    //	v8::SetResourceConstraints(&constraints);
    //}
    //isolate_->Exit();

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    HandleScope scope(isolate_);

    // Setup the template we'll use for all CLR object references.
    auto obj_template = ObjectTemplate::New(isolate_);
    obj_template->SetInternalFieldCount(1);
    obj_template->SetHandler(
        NamedPropertyHandlerConfiguration(
            ClrObjectRef::GetPropertyValueCallback,
            ClrObjectRef::SetPropertyValueCallback,
            nullptr,
            ClrObjectRef::DeletePropertyCallback,
            ClrObjectRef::EnumeratePropertiesCallback
        )
    );
    obj_template->SetCallAsFunctionHandler(ClrObjectRef::InvokeCallback);

    // TODO: the "valueOf" callback was originally set on the prototype i.e. :
    // fo->PrototypeTemplate()->Set(isolate_, "valueOf", FunctionTemplate::New(isolate_, ClrObjectRef::ValueOfCallback));
    // Is there some advantage to doing that? AFAICT, this achieves the same result.
    // Note that interceptors get priority over accessors, so these methods can be provided by the GetPropertyValue callback
    // if desired. The reason for having dedicated callbacks is to guarantee a sane implementation of these
    // methods exists on all objects, in case the GetPropertyValue callback chooses to ignore these properties.
    obj_template->Set(isolate_, "valueOf", FunctionTemplate::New(isolate_, ClrObjectRef::ValueOfCallback));
    obj_template->Set(isolate_, "toString", FunctionTemplate::New(isolate_, ClrObjectRef::ToStringCallback));

    clrObjectTemplate_ = new Persistent<ObjectTemplate>(isolate_, obj_template);

    global_context_ = new Persistent<Context>(isolate_, Context::New(isolate_));

    // Do this last, in case anything above fails
    INCREMENT(js_mem_debug_engine_count);
}

Persistent<Script>* JsEngine::CompileScript(const uint16_t* str, const uint16_t* resourceName, jsvalue* error)
{
    assert(str != nullptr);

    // todo: change this to use v8::ScriptCompiler::CompileUnboundScript() if the goal is to have a 
    // compiled script that could be used with any context.

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    HandleScope scope(isolate_);
    Context::Scope contextScope(Local<Context>::New(isolate_, *global_context_));

    TryCatch trycatch(isolate_);

    auto source = String::NewFromTwoByte(isolate_, str).ToLocalChecked();

    auto res_name = resourceName != NULL
        ? String::NewFromTwoByte(isolate_, resourceName).ToLocalChecked()
        : String::Empty(isolate_);

    ScriptOrigin scriptOrigin(res_name);

    Local<Script> script;
    if (!Script::Compile(isolate_->GetCurrentContext(), source, &scriptOrigin).ToLocal(&script))
    {
        // Compilation failed e.g. syntax error
        // TODO: this can't possibly work right - the retval from JsValue::ForError is a stack var
        //*error = JsValue::ForError(trycatch);
        // todo: should we not just return here? e.g. return null
    }

    return new Persistent<Script>(isolate_, script);
}


void JsEngine::TerminateExecution()
{
    isolate_->TerminateExecution();
}

void JsEngine::DumpHeapStats()
{
    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);

    // gc first.
    // TODO: IdleNotification no longer exists?
    //while(!V8::IdleNotification()) {};

    HeapStatistics stats;
    isolate_->GetHeapStatistics(&stats);
    std::wcout << "Heap size limit " << (stats.heap_size_limit() / Mega) << std::endl;
    std::wcout << "Total heap size " << (stats.total_heap_size() / Mega) << std::endl;
    std::wcout << "Heap size executable " << (stats.total_heap_size_executable() / Mega) << std::endl;
    std::wcout << "Total physical size " << (stats.total_physical_size() / Mega) << std::endl;
    std::wcout << "Used heap size " << (stats.used_heap_size() / Mega) << std::endl;
}

JsContext* JsEngine::NewContext(int32_t id)
{
    return new JsContext(id, this);
}

void JsEngine::Dispose()
{
    if (isolate_ != NULL) {
        isolate_->Enter();

        clrObjectTemplate_->Reset();
        delete clrObjectTemplate_;
        clrObjectTemplate_ = NULL;

        global_context_->Reset();
        delete global_context_;
        global_context_ = NULL;

        isolate_->Exit();
        isolate_->Dispose();
        // Isolates can only be Dispose()'d, not deleted
        isolate_ = NULL;

        delete allocator_;
        allocator_ = NULL;

        memset(&callbacks_, 0, sizeof(jscallbacks));
    }
}

void JsEngine::DisposeObject(Persistent<Object>* obj)
{
    assert(obj != nullptr);

    // todo: not sure we actually need this stuff just to Reset a Persistent handle
    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);

    obj->Reset();
}




