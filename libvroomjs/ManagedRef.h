#pragma once

#include "vroomjs.h"

class JsContext;

class ManagedRef {
public:
	inline explicit ManagedRef(JsContext* context, int id) :
		context_(context),
		id_(id)
	{
		INCREMENT(js_mem_debug_managedref_count);
	}

	inline int32_t Id() { return id_; }

	Local<Value> GetPropertyValue(Local<String> name);
	Local<Value> SetPropertyValue(Local<String> name, Local<Value> value);
	Local<Value> GetValueOf();
	Local<Value> Invoke(const FunctionCallbackInfo<Value>& args);
	Local<Boolean> DeleteProperty(Local<String> name);
	Local<Array> EnumerateProperties();

	~ManagedRef();

private:
	ManagedRef() {
		INCREMENT(js_mem_debug_managedref_count);
	}
	JsContext* context_;
	int32_t id_;
};
