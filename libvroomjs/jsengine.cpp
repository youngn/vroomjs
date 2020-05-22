
#include <cstring>
#include <iostream>
#include <cassert>
#include "vroomjs.h"

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

JsEngine* JsEngine::New(int32_t max_young_space = -1, int32_t max_old_space = -1)
{
    auto engine = new JsEngine();
    engine->allocator_ = v8::ArrayBuffer::Allocator::NewDefaultAllocator();

    Isolate::CreateParams create_params;
    create_params.array_buffer_allocator = engine->allocator_;

    auto pIsolate = Isolate::New(create_params);
    engine->isolate_ = pIsolate;

    // todo: SetResourceConstraints doesn't seem to exist anymore
    //engine->isolate_->Enter();
    //if (max_young_space > 0 && max_old_space > 0) {
    //	v8::ResourceConstraints constraints;
    //	constraints.set_max_young_space_size(max_young_space * Mega);
    //	constraints.set_max_old_space_size(max_old_space * Mega);
    //
    //	v8::SetResourceConstraints(&constraints);
    //}
    //engine->isolate_->Exit();

    Locker locker(engine->isolate_);
    Isolate::Scope isolate_scope(engine->isolate_);
    HandleScope scope(engine->isolate_);

    // Setup the template we'll use for all managed object references.
    auto fo = FunctionTemplate::New(engine->isolate_);
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
    engine->managed_template_ = new Persistent<FunctionTemplate>(engine->isolate_, fo);

    auto fp = FunctionTemplate::New(engine->isolate_, managed_valueof);
    engine->valueof_function_template_ =
        new Persistent<FunctionTemplate>(engine->isolate_, fp);

    engine->global_context_ =
        new Persistent<Context>(engine->isolate_, Context::New(engine->isolate_));

    auto ctx = Local<Context>::New(engine->isolate_, *engine->global_context_);
    Context::Scope contextScope(ctx);

    fo->PrototypeTemplate()->Set(engine->isolate_, "valueOf", fp);
    return engine;
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
        // TODO: this can't possibly work right - the retval from ErrorFromV8 is a stack var
        *error = ErrorFromV8(trycatch);
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

	    keepalive_remove_ = NULL;
		keepalive_get_property_value_ = NULL;
		keepalive_set_property_value_ = NULL;
		keepalive_valueof_ = NULL;
		keepalive_invoke_ = NULL;
		keepalive_delete_property_ = NULL;
		keepalive_enumerate_properties_ = NULL;
	}
}

void JsEngine::DisposeObject(Persistent<Object>* obj)
{
    assert(obj != nullptr);

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    
	obj->Reset();
}

jsvalue JsEngine::ErrorFromV8(TryCatch& trycatch)
{
    jsvalue v;

    HandleScope scope(isolate_);
    
    auto exception = trycatch.Exception();

    v.type = JSVALUE_TYPE_UNKNOWN_ERROR;
    v.value.str = 0;
    v.length = 0;

    // If this is a managed exception we need to place its ID inside the jsvalue
    // and set the type JSVALUE_TYPE_MANAGED_ERROR to make sure the CLR side will
    // throw on it.

    if (exception->IsObject()) {
        auto obj = Local<Object>::Cast(exception);
        if (obj->InternalFieldCount() == 1) {
        Local<External> wrap = Local<External>::Cast(obj->GetInternalField(0));
        ManagedRef* ref = (ManagedRef*)wrap->Value();
        v.type = JSVALUE_TYPE_MANAGED_ERROR;
        v.length = ref->Id();
        return v;
        }
    }

	jserror *error = new jserror();
	memset(error, 0, sizeof(jserror));
	
	Local<Message> message = trycatch.Message();

	if (!message.IsEmpty()) {
		error->line = message->GetLineNumber(isolate_->GetCurrentContext()).FromMaybe(0);
		error->column = message->GetStartColumn();
		error->resource = AnyFromV8(message->GetScriptResourceName());
		error->message = AnyFromV8(message->Get());
	}
	if (exception->IsObject()) {
        Local<Object> obj2 = Local<Object>::Cast(exception);
	    error->type = AnyFromV8(obj2->GetConstructorName());
	}

	error->exception = AnyFromV8(exception);
	v.type = JSVALUE_TYPE_ERROR;
	v.value.ptr = error;
    
	return v;
}
    
jsvalue JsEngine::StringFromV8(Local<String> value)
{
    // From how this is used in the code, the assumption
    // seems to be that the value will not be empty.
    assert(!value.IsEmpty());

    jsvalue v;
    v.length = value->Length();

    // todo: is this the best way to convert?
    v.value.str = new uint16_t[v.length+1];
    value->Write(isolate_, v.value.str);
    v.type = JSVALUE_TYPE_STRING;

    return v;
}   

jsvalue JsEngine::WrappedFromV8(Local<Object> obj)
{
    jsvalue v;
       
    v.type = JSVALUE_TYPE_WRAPPED;
    v.length = 0;
    v.value.ptr = new Persistent<Object>(isolate_, obj);
    
    return v;
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
    
jsvalue JsEngine::AnyFromV8(Local<Value> value, Local<Object> thisArg)
{
    jsvalue v;
    
    // Initialize to a generic error.
    v.type = JSVALUE_TYPE_UNKNOWN_ERROR;
    v.length = 0;
    v.value.str = 0;
    
    if (value->IsNull() || value->IsUndefined()) {
        v.type = JSVALUE_TYPE_NULL;
    }                
    else if (value->IsBoolean()) {
        v.type = JSVALUE_TYPE_BOOLEAN;
        v.value.i32 = value->BooleanValue(isolate_) ? 1 : 0;
    }
    else if (value->IsInt32()) {
        v.type = JSVALUE_TYPE_INTEGER;
        v.value.i32 = value->Int32Value(isolate_->GetCurrentContext()).FromMaybe(0);
    }
    else if (value->IsUint32()) {
        v.type = JSVALUE_TYPE_INDEX;
        v.value.i64 = value->Uint32Value(isolate_->GetCurrentContext()).FromMaybe(0);
    }
    else if (value->IsNumber()) {
        v.type = JSVALUE_TYPE_NUMBER;
        v.value.num = value->NumberValue(isolate_->GetCurrentContext()).FromMaybe(0.0);
    }
    else if (value->IsString()) {
        v = StringFromV8(Local<String>::Cast(value));
    }
    else if (value->IsDate()) {
        v.type = JSVALUE_TYPE_DATE;
        v.value.num = value->NumberValue(isolate_->GetCurrentContext()).FromMaybe(0);
    }
    else if (value->IsArray()) {
        auto arr = Local<Array>::Cast(value);
        v.length = arr->Length();
        jsvalue* array = new jsvalue[v.length];
        for (int i = 0; i < v.length; i++) {
            // todo: Get returns a MaybeLocal... could it possibly be empty?
            auto x = arr->Get(isolate_->GetCurrentContext(), i).ToLocalChecked();
            array[i] = AnyFromV8(x);
        }
        v.type = JSVALUE_TYPE_ARRAY;
        v.value.arr = array;
    }
    else if (value->IsFunction()) {
		auto function = Local<Function>::Cast(value);
		jsvalue* array = new jsvalue[2];
        array[0].value.ptr = new Persistent<Function>(Isolate::GetCurrent(), function);
        array[0].length = 0;
        array[0].type = JSVALUE_TYPE_WRAPPED;
        if (!thisArg.IsEmpty()) {
            array[1].value.ptr = new Persistent<Object>(Isolate::GetCurrent(), thisArg);
            array[1].length = 0;
            array[1].type = JSVALUE_TYPE_WRAPPED;
        }
        else {
            array[1].value.ptr = NULL;
            array[1].length = 0;
            array[1].type = JSVALUE_TYPE_NULL;
        }
        v.type = JSVALUE_TYPE_FUNCTION;
        v.value.arr = array;
    }
    else if (value->IsObject()) {
        auto obj = Local<Object>::Cast(value);
        if (obj->InternalFieldCount() == 1)
            v = ManagedFromV8(obj);
        else
            v = WrappedFromV8(obj);
    }

    // todo: throw?
    return v;
}

jsvalue JsEngine::ArrayFromArguments(const FunctionCallbackInfo<Value>& args)
{
    jsvalue v = jsvalue_alloc_array(args.Length());
    auto thisArg = args.Holder();

    for (int i=0 ; i < v.length ; i++) {
        v.value.arr[i] = AnyFromV8(args[i], thisArg);
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

Local<Value> JsEngine::AnyToV8(jsvalue v, int32_t contextId)
{
	if (v.type == JSVALUE_TYPE_EMPTY) {
		return Local<Value>();
	}
	if (v.type == JSVALUE_TYPE_NULL) {
        return Null(isolate_);
    }
    if (v.type == JSVALUE_TYPE_BOOLEAN) {
        return Boolean::New(isolate_, v.value.i32 != 0);
    }
    if (v.type == JSVALUE_TYPE_INTEGER) {
        return Int32::New(isolate_, v.value.i32);
    }
    if (v.type == JSVALUE_TYPE_NUMBER) {
        return Number::New(isolate_, v.value.num);
    }
    if (v.type == JSVALUE_TYPE_STRING) {
        return String::NewFromTwoByte(isolate_, v.value.str).ToLocalChecked();
    }
    if (v.type == JSVALUE_TYPE_DATE) {
        return Date::New(isolate_->GetCurrentContext(), v.value.num).ToLocalChecked();
        return Local<Object>::New(isolate_, *pObj);
    }
	
    // Arrays are converted to JS native arrays.
    if (v.type == JSVALUE_TYPE_ARRAY) {
        auto arr = Array::New(isolate_, v.length);
        for(int i = 0; i < v.length; i++) {
            arr->Set(isolate_->GetCurrentContext(), i, AnyToV8(v.value.arr[i], contextId));
        }
        return arr;        
    }
        
    // This is an ID to a managed object that lives inside the JsContext keep-alive
    // cache. We just wrap it and the pointer to the engine inside an External. A
    // managed error is still a CLR object so it is wrapped exactly as a normal
    // managed object.
    if (v.type == JSVALUE_TYPE_MANAGED || v.type == JSVALUE_TYPE_MANAGED_ERROR) {

		auto ref = new ManagedRef(this, contextId, v.length);
        auto t = Local<FunctionTemplate>::New(isolate_, *managed_template_);

		auto obj = t->InstanceTemplate()->NewInstance(isolate_->GetCurrentContext()).ToLocalChecked();
        obj->SetInternalField(0, External::New(isolate_, ref));

        // todo: not sure if any of this is needed, revisit
		//Persistent<Object> persistent = Persistent<Object>::New(object);
		//persistent->SetInternalField(0, External::New(ref));
		//persistent.MakeWeak(NULL, managed_destroy);
        //return persistent;
        return obj;
    }

    // todo: throw?
    return Null(isolate_);
}

int32_t JsEngine::ArrayToV8Args(jsvalue value, int32_t contextId, Local<Value> preallocatedArgs[])
{
    if (value.type != JSVALUE_TYPE_ARRAY)
        return -1;
        
    for (int i=0 ; i < value.length ; i++) {
        preallocatedArgs[i] = AnyToV8(value.value.arr[i], contextId);
    }
    
    return value.length;
}
