#pragma once

#include "vroomjs.h"

// JsEngine is a single isolated v8 interpreter and is the referenced as an IntPtr
// by the JsEngine on the CLR side.
class JsContext;

class JsEngine {
public:
	JsEngine(int32_t max_young_space, int32_t max_old_space, jscallbacks callbacks);
	void TerminateExecution();

	// Call delegates into managed code.
	inline void CallRemove(int32_t context, int id) {
		if (callbacks_.remove == NULL) {
			return;
		}
		callbacks_.remove(context, id);
	}
	inline jsvalue CallGetPropertyValue(int32_t context, int32_t id, uint16_t* name) {
		if (callbacks_.get_property_value == NULL) {
			jsvalue v;
			v.type == JSVALUE_TYPE_NULL;
			return v;
		}
		jsvalue value = callbacks_.get_property_value(context, id, name);
		return value;
	}
	inline jsvalue CallSetPropertyValue(int32_t context, int32_t id, uint16_t* name, jsvalue value) {
		if (callbacks_.set_property_value == NULL) {
			jsvalue v;
			v.type == JSVALUE_TYPE_NULL;
			return v;
		}
		return callbacks_.set_property_value(context, id, name, value);
	}
	inline jsvalue CallValueOf(int32_t context, int32_t id) {
		if (callbacks_.valueof == NULL) {
			jsvalue v;
			v.type == JSVALUE_TYPE_NULL;
			return v;
		}
		return callbacks_.valueof(context, id);
	}
	inline jsvalue CallInvoke(int32_t context, int32_t id, jsvalue args) {
		if (callbacks_.invoke == NULL) {
			jsvalue v;
			v.type == JSVALUE_TYPE_NULL;
			return v;
		}
		return callbacks_.invoke(context, id, args);
	}
	inline jsvalue CallDeleteProperty(int32_t context, int32_t id, uint16_t* name) {
		if (callbacks_.delete_property == NULL) {
			jsvalue v;
			v.type == JSVALUE_TYPE_NULL;
			return v;
		}
		jsvalue value = callbacks_.delete_property(context, id, name);
		return value;
	}
	inline jsvalue CallEnumerateProperties(int32_t context, int32_t id) {
		if (callbacks_.enumerate_properties == NULL) {
			jsvalue v;
			v.type == JSVALUE_TYPE_NULL;
			return v;
		}
		jsvalue value = callbacks_.enumerate_properties(context, id);
		return value;
	}

	// Conversions. Note that all the conversion functions should be called
	// with an HandleScope already on the stack or sill misarabily fail.
	jsvalue ManagedFromV8(Local<Object> obj);

	Persistent<Script>* CompileScript(const uint16_t* str, const uint16_t* resourceName, jsvalue* error);

	// Converts JS function Arguments to an array of jsvalue to call managed code.
	jsvalue ArrayFromArguments(const FunctionCallbackInfo<Value>& args);

	// Dispose a Persistent<Object> that was pinned on the CLR side by JsObject.
	void DisposeObject(Persistent<Object>* obj);

	void Dispose();

	void DumpHeapStats();
	Isolate* Isolate() { return isolate_; }
	JsContext* NewContext(int32_t id);

	Local<FunctionTemplate> Template() {
		return Local<FunctionTemplate>::New(isolate_, *managed_template_);
	}

	inline virtual ~JsEngine() {
		DECREMENT(js_mem_debug_engine_count);
	}


	Persistent<Context>* global_context_;

private:
	v8::Isolate* isolate_;
	ArrayBuffer::Allocator* allocator_;

	Persistent<FunctionTemplate>* managed_template_;
	Persistent<FunctionTemplate>* valueof_function_template_;

	jscallbacks callbacks_;
};

