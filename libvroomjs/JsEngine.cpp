#include <cstring>
#include <iostream>
#include <cassert>

#include "vroomjs.h"
#include "JsEngine.h"
#include "ManagedRef.h"
#include "JsContext.h"


long js_mem_debug_engine_count;

extern "C" jsvalue CALLINGCONVENTION jsvalue_alloc_array(const int32_t length);

static const int Mega = 1024 * 1024;


static void managed_prop_get(Local<Name> name, const PropertyCallbackInfo<Value>& info)
{
#ifdef DEBUG_TRACE_API
		std::cout << "managed_prop_get" << std::endl;
#endif
    auto isolate = Isolate::GetCurrent();
    HandleScope scope(isolate); // todo: is this needed, or is it already implied?

    auto self = info.Holder();
    auto wrap = Local<External>::Cast(self->GetInternalField(0));
    auto ref = (ManagedRef*)wrap->Value();

    auto nameStr = name->ToString(isolate->GetCurrentContext()).ToLocalChecked();
    auto propValue = ref->GetPropertyValue(nameStr);
    info.GetReturnValue().Set(propValue);
}

static void managed_prop_set(Local<Name> name, Local<Value> value, const PropertyCallbackInfo<Value>& info)
{
#ifdef DEBUG_TRACE_API
		std::cout << "managed_prop_set" << std::endl;
#endif
    auto isolate = Isolate::GetCurrent();
    HandleScope scope(isolate); // todo: is this needed, or is it already implied?

    auto self = info.Holder();
    auto wrap = Local<External>::Cast(self->GetInternalField(0));
    auto ref = (ManagedRef*)wrap->Value();

    // TODO: could this really ever be null?
    if (ref == NULL)
        return;

    auto nameStr = name->ToString(isolate->GetCurrentContext()).ToLocalChecked();

    // TODO: SetPropertyValue doesn't need to return anything
    auto result = ref->SetPropertyValue(nameStr, value);
}

static void managed_prop_delete(Local<Name> name, const PropertyCallbackInfo<Boolean>& info)
{
#ifdef DEBUG_TRACE_API
		std::cout << "managed_prop_delete" << std::endl;
#endif
    auto isolate = Isolate::GetCurrent();
    HandleScope scope(isolate); // todo: is this needed, or is it already implied?

    auto self = info.Holder();
    auto wrap = Local<External>::Cast(self->GetInternalField(0));
    auto ref = (ManagedRef*)wrap->Value();

    auto nameStr = name->ToString(isolate->GetCurrentContext()).ToLocalChecked();
    auto retVal = ref->DeleteProperty(nameStr);

    // todo: check docs if we really need to be returning a value here
    info.GetReturnValue().Set(retVal);
}

static void managed_prop_enumerate(const PropertyCallbackInfo<Array>& info)
{
#ifdef DEBUG_TRACE_API
		std::cout << "managed_prop_enumerate" << std::endl;
#endif
    auto isolate = Isolate::GetCurrent();
    HandleScope scope(isolate); // todo: is this needed, or is it already implied?

    auto self = info.Holder();
    auto wrap = Local<External>::Cast(self->GetInternalField(0));
    auto ref = (ManagedRef*)wrap->Value();

    info.GetReturnValue().Set(ref->EnumerateProperties());
}

static void managed_call(const FunctionCallbackInfo<Value>& args)
{
#ifdef DEBUG_TRACE_API
		std::cout << "managed_call" << std::endl;
#endif
    auto isolate = Isolate::GetCurrent();
    HandleScope scope(isolate); // todo: is this needed, or is it already implied?

    auto self = args.Holder();
    auto wrap = Local<External>::Cast(self->GetInternalField(0));
    auto ref = (ManagedRef*)wrap->Value();

    args.GetReturnValue().Set(ref->Invoke(args));
}

static void managed_valueof(const FunctionCallbackInfo<Value>& args)
{
#ifdef DEBUG_TRACE_API
		std::cout << "managed_valueof" << std::endl;
#endif
    auto isolate = Isolate::GetCurrent();
    HandleScope scope(isolate); // todo: is this needed, or is it already implied?

    auto self = args.Holder();
    auto wrap = Local<External>::Cast(self->GetInternalField(0));
    auto ref = (ManagedRef*)wrap->Value();

    args.GetReturnValue().Set(ref->GetValueOf());
}

JsEngine::JsEngine(int32_t max_young_space, int32_t max_old_space, jscallbacks callbacks)
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

    // Setup the template we'll use for all managed object references.
    auto fo = FunctionTemplate::New(isolate_);
    auto obj_template = fo->InstanceTemplate();
    obj_template->SetInternalFieldCount(1);
    obj_template->SetHandler(
        NamedPropertyHandlerConfiguration(
            managed_prop_get,
            managed_prop_set,
            nullptr,
            managed_prop_delete,
            managed_prop_enumerate
        )
    );
    obj_template->SetCallAsFunctionHandler(managed_call);
    managed_template_ = new Persistent<FunctionTemplate>(isolate_, fo);

    auto fp = FunctionTemplate::New(isolate_, managed_valueof);
    valueof_function_template_ =
        new Persistent<FunctionTemplate>(isolate_, fp);

    global_context_ =
        new Persistent<Context>(isolate_, Context::New(isolate_));

    auto ctx = Local<Context>::New(isolate_, *global_context_);
    Context::Scope contextScope(ctx);

    fo->PrototypeTemplate()->Set(isolate_, "valueOf", fp);

    callbacks_ = callbacks;

    // Do this last, in case anything above fails
    INCREMENT(js_mem_debug_engine_count);
}

Persistent<Script> *JsEngine::CompileScript(const uint16_t* str, const uint16_t *resourceName, jsvalue *error)
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

		managed_template_->Reset();
		delete managed_template_;
		managed_template_ = NULL;
	
		valueof_function_template_->Reset();
		delete valueof_function_template_;
		valueof_function_template_ = NULL;

		global_context_->Reset();
    	delete global_context_;
		global_context_ = NULL;

		isolate_->Exit();
		isolate_->Dispose();
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

jsvalue JsEngine::ManagedFromV8(Local<Object> obj)
{
    jsvalue v;
    
	auto wrap = Local<External>::Cast(obj->GetInternalField(0));
    auto ref = (ManagedRef*)wrap->Value();
	v.type = JSVALUE_TYPE_MANAGED;
    v.length = ref->Id();
    v.value.str = 0;

    return v;
}
    
jsvalue JsEngine::ArrayFromArguments(const FunctionCallbackInfo<Value>& args)
{
    jsvalue v = jsvalue_alloc_array(args.Length());
    auto thisArg = args.Holder();

    for (int i=0 ; i < v.length ; i++) {
        //v.value.arr[i] = convert_->AnyFromV8(args[i], thisArg);
    }
    
    return v;
}

static void managed_destroy(const WeakCallbackInfo<Local<Object>>& info)
{
#ifdef DEBUG_TRACE_API
		std::cout << "managed_destroy" << std::endl;
#endif
    // todo: fix this
    HandleScope scope(info.GetIsolate());

    // todo:  GetInternalField here just returns a void*
    // are we to assume this points directly to managed obj?
    auto x = info.GetInternalField(0);
 //   Persistent<Object> self = Persistent<Object>::Cast(object);
 //   Local<External> wrap = Local<External>::Cast(self->GetInternalField(0));
 //   ManagedRef* ref = (ManagedRef*)wrap->Value();
    ManagedRef* ref = (ManagedRef*)x;
    delete ref;
 //   object.Dispose();
}

