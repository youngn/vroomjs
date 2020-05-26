// This file is part of the VroomJs library.
//
// Author:
//     Federico Di Gregorio <fog@initd.org>
//
// Copyright (c) 2013 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#ifndef LIBVROOMJS_H
#define LIBVROOMJS_H 

#include <v8.h>
#include <stdlib.h>
#include <stdint.h>
#include <iostream>
#include <cassert>

using namespace v8;

// jsvalue (JsValue on the CLR side) is a struct that can be easily marshaled
// by simply blitting its value (being only 16 bytes should be quite fast too).

#define JSVALUE_TYPE_UNKNOWN_ERROR  -1
#define JSVALUE_TYPE_EMPTY			 0
#define JSVALUE_TYPE_NULL            1
#define JSVALUE_TYPE_BOOLEAN         2
#define JSVALUE_TYPE_INTEGER         3
#define JSVALUE_TYPE_NUMBER          4
#define JSVALUE_TYPE_STRING          5
#define JSVALUE_TYPE_DATE            6
#define JSVALUE_TYPE_INDEX           7
#define JSVALUE_TYPE_ARRAY          10
#define JSVALUE_TYPE_STRING_ERROR   11
#define JSVALUE_TYPE_MANAGED        12
#define JSVALUE_TYPE_MANAGED_ERROR  13
#define JSVALUE_TYPE_JSOBJECT       14
#define JSVALUE_TYPE_DICT           15
#define JSVALUE_TYPE_ERROR          16
#define JSVALUE_TYPE_FUNCTION       17
#define JSVALUE_TYPE_JSARRAY        18

#ifdef _WIN32 
#define EXPORT __declspec(dllexport)
#else 
#define EXPORT
#endif

#ifdef _WIN32
#include "Windows.h"
#define CALLINGCONVENTION __stdcall
#define INCREMENT(x) InterlockedIncrement(&x)
#define DECREMENT(x) InterlockedDecrement(&x)
#else 
#define CALLINGCONVENTION
#define INCREMENT(x) __sync_fetch_and_add(&x, 1)
#define DECREMENT(x) __sync_fetch_and_add(&x, -1)
#endif

extern int32_t js_object_marshal_type;

extern long js_mem_debug_engine_count;
extern long js_mem_debug_context_count;
extern long js_mem_debug_managedref_count;
extern long js_mem_debug_script_count;

extern "C" 
{
    struct jsvalue
    {
        // 8 bytes is the maximum CLR alignment; by putting the union first and a
        // int64_t inside it we make (almost) sure the offset of 'type' will always
        // be 8 and the total size 16. We add a check to JsContext_new anyway.
        
        union 
        {
            int32_t     i32;
            int64_t     i64;
            double      num;
            void*       ptr;
            uint16_t*   str;
            jsvalue*    arr;
        } value;
        
        int32_t         type;
        int32_t         length; // Also used as slot index on the CLR side.
	};
 
	struct jserror
	{
		jsvalue type;
		int32_t line;
		int32_t column;
		jsvalue resource;
		jsvalue message;
		jsvalue exception;
	};
	
	EXPORT void CALLINGCONVENTION jsvalue_dispose(jsvalue value);
}

class JsEngine;
class JsContext;

// The only way for the C++/V8 side to call into the CLR is to use the function
// pointers (CLR, delegates) defined below.

extern "C" 
{
    // We don't have a keepalive_add_f because that is managed on the managed side.
    // Its definition would be "int (*keepalive_add_f) (ManagedRef obj)".
    
    typedef void (CALLINGCONVENTION *keepalive_remove_f) (int context, int id);
    typedef jsvalue (CALLINGCONVENTION *keepalive_get_property_value_f) (int context, int id, uint16_t* name);
    typedef jsvalue (CALLINGCONVENTION *keepalive_set_property_value_f) (int context, int id, uint16_t* name, jsvalue value);
    typedef jsvalue (CALLINGCONVENTION *keepalive_valueof_f) (int context, int id);
	typedef jsvalue (CALLINGCONVENTION *keepalive_invoke_f) (int context, int id, jsvalue args);
	typedef jsvalue (CALLINGCONVENTION *keepalive_delete_property_f) (int context, int id, uint16_t* name);
	typedef jsvalue (CALLINGCONVENTION *keepalive_enumerate_properties_f) (int context, int id);

	struct jscallbacks
	{
		keepalive_remove_f remove;
		keepalive_get_property_value_f get_property_value;
		keepalive_set_property_value_f set_property_value;
		keepalive_valueof_f valueof;
		keepalive_invoke_f invoke;
		keepalive_delete_property_f delete_property;
		keepalive_enumerate_properties_f enumerate_properties;
	};
}

class JsValue {
public:
	static JsValue ForUnknownError() {
		return JsValue(JSVALUE_TYPE_UNKNOWN_ERROR, 0, 0);
	}
	static JsValue ForNull() {
		return JsValue(JSVALUE_TYPE_NULL, 0, 0);
	}
	static JsValue ForBoolean(bool value) {
		return JsValue(JSVALUE_TYPE_BOOLEAN, 0, (int32_t)value);
	}
	static JsValue ForInt32(int32_t value) {
		return JsValue(JSVALUE_TYPE_INTEGER, 0, value);
	}
	static JsValue ForUInt32(uint32_t value) {
		return JsValue(JSVALUE_TYPE_INDEX, 0, (int64_t)value);
	}
	static JsValue ForNumber(double value) {
		return JsValue(JSVALUE_TYPE_NUMBER, 0, value);
	}
	static JsValue ForString(int32_t length, uint16_t* value) {
		assert(value != nullptr);
		return JsValue(JSVALUE_TYPE_STRING, length, value);
	}
	static JsValue ForDate(double value) {
		return JsValue(JSVALUE_TYPE_DATE, 0, value);
	}
	static JsValue ForJsArray(Persistent<Array>* value) {
		assert(value != nullptr);
		return JsValue(JSVALUE_TYPE_JSARRAY, 0, (void*)value);
	}
	static JsValue ForJsFunction(Persistent<Function>* value) {
		assert(value != nullptr);
		return JsValue(JSVALUE_TYPE_FUNCTION, 0, (void*)value);
	}
	static JsValue ForJsObject(Persistent<Object>* value) {
		assert(value != nullptr);
		return JsValue(JSVALUE_TYPE_JSOBJECT, 0, (void*)value);
	}
	static JsValue ForError(jserror* value) {
		assert(value != nullptr);
		return JsValue(JSVALUE_TYPE_ERROR, 0, (void*)value);
	}
	static JsValue ForManagedError(int32_t id) {
		return JsValue(JSVALUE_TYPE_MANAGED_ERROR, id, 0);
	}
	static JsValue ForManagedObject(int32_t id) {
		return JsValue(JSVALUE_TYPE_MANAGED, id, 0);
	}

	inline int32_t ValueType() const {
		return v.type;
	}

	inline bool BooleanValue() const {
		assert(v.type == JSVALUE_TYPE_BOOLEAN);
		return v.value.i32 != 0;
	}
	inline int32_t Int32Value() const {
		assert(v.type == JSVALUE_TYPE_INTEGER);
		return v.value.i32;
	}
	inline uint32_t UInt32Value() const {
		assert(v.type == JSVALUE_TYPE_INDEX);
		return (uint32_t)v.value.i64;
	}
	inline double NumberValue() const {
		assert(v.type == JSVALUE_TYPE_NUMBER);
		return v.value.num;
	}
	inline uint16_t* StringValue() const {
		assert(v.type == JSVALUE_TYPE_STRING);
		return v.value.str;
	}
	inline double DateValue() const {
		assert(v.type == JSVALUE_TYPE_DATE);
		return v.value.num;
	}
	inline Persistent<Array>* JsArrayValue() const {
		assert(v.type == JSVALUE_TYPE_JSARRAY);
		return (Persistent<Array>*)v.value.ptr;
	}
	inline Persistent<Function>* JsFunctionValue() const {
		assert(v.type == JSVALUE_TYPE_FUNCTION);
		return (Persistent<Function>*)v.value.ptr;
	}
	inline Persistent<Object>* JsObjectValue() const {
		assert(v.type == JSVALUE_TYPE_JSOBJECT);
		return (Persistent<Object>*)v.value.ptr;
	}

	operator jsvalue() const {
		return v;
	}

	inline JsValue(const jsvalue& value) {
		v = value;
	}

private:
	inline JsValue(int32_t type, int32_t length, int32_t i32) {
		v.type = type;
		v.length = length;
		v.value.i32 = i32;
	}
	inline JsValue(int32_t type, int32_t length, int64_t i64) {
		v.type = type;
		v.length = length;
		v.value.i64 = i64;
	}
	inline JsValue(int32_t type, int32_t length, double num) {
		v.type = type;
		v.length = length;
		v.value.num = num;
	}
	inline JsValue(int32_t type, int32_t length, void* ptr) {
		v.type = type;
		v.length = length;
		v.value.ptr = ptr;
	}
	inline JsValue(int32_t type, int32_t length, uint16_t* str) {
		v.type = type;
		v.length = length;
		v.value.str = str;
	}

	jsvalue v;
};

class JsConvert {
public:
	JsConvert(Isolate* isolate);

	// Conversions. Note that all the conversion functions should be called
	// with an HandleScope already on the stack or will miserably fail.
	// todo: consider adding inner HandleScope so the above comment is moot??
	JsValue AnyFromV8(Local<Value> value, Local<Object> thisArg = Local<Object>()) const;
	Local<Value> AnyToV8(JsValue value, int32_t contextId) const;
	JsValue ErrorFromV8(TryCatch& trycatch) const;

private:
	Isolate* isolate_;
};


class JsScript {
public:
	static JsScript *New(JsEngine *engine);
	
	jsvalue Compile(const uint16_t* str, const uint16_t *resourceName);
	void Dispose();
	Persistent<Script> *GetScript() { return script_; }

	inline virtual ~JsScript() {
		DECREMENT(js_mem_debug_script_count);
	}

private:
	inline JsScript() {
		INCREMENT(js_mem_debug_script_count);
	}
	JsEngine *engine_;
	Persistent<Script> *script_;
};

// JsEngine is a single isolated v8 interpreter and is the referenced as an IntPtr
// by the JsEngine on the CLR side.
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
   
	Persistent<Script> *CompileScript(const uint16_t* str, const uint16_t *resourceName, jsvalue *error);

	// Converts JS function Arguments to an array of jsvalue to call managed code.
    jsvalue ArrayFromArguments(const FunctionCallbackInfo<Value>& args);

	// Dispose a Persistent<Object> that was pinned on the CLR side by JsObject.
    void DisposeObject(Persistent<Object>* obj);

	void Dispose();
	
	void DumpHeapStats();
	Isolate *GetIsolate() { return isolate_; }
	JsContext* NewContext(int32_t id);

	inline virtual ~JsEngine() {
		DECREMENT(js_mem_debug_engine_count);
	}


	Persistent<Context> *global_context_;

private:
	Isolate* isolate_;
	ArrayBuffer::Allocator* allocator_;
	
	Persistent<FunctionTemplate> *managed_template_;
	Persistent<FunctionTemplate> *valueof_function_template_;

	jscallbacks callbacks_;

	JsConvert* convert_;
};


class JsContext {
 public:
	JsContext(int32_t id, JsEngine* engine, JsConvert* convert);

    // Called by bridge to execute JS from managed code.
	JsValue Execute(const uint16_t* str, const uint16_t *resourceName);
	JsValue Execute(JsScript *script);

	JsValue GetGlobal();
	JsValue GetVariable(const uint16_t* name);
	JsValue SetVariable(const uint16_t* name, jsvalue value);
	JsValue GetPropertyNames(Persistent<Object>* obj);
	JsValue GetPropertyValue(Persistent<Object>* obj, const uint16_t* name);
	JsValue GetPropertyValue(Persistent<Object>* obj, const uint32_t index);
	JsValue SetPropertyValue(Persistent<Object>* obj, const uint16_t* name, jsvalue value);
	JsValue SetPropertyValue(Persistent<Object>* obj, const uint32_t index, jsvalue value);
	JsValue InvokeFunction(Persistent<Function>* func, jsvalue receiver, int argCount, jsvalue* args);

	void Dispose();
     
	inline int32_t GetId() {
		return id_;
	}

	inline virtual ~JsContext() {
		DECREMENT(js_mem_debug_context_count);
	}

 private:             
	int32_t id_;
	JsEngine* engine_;
	JsConvert* convert_;
	Isolate *isolate_;
	Persistent<Context> *context_;
};


class ManagedRef {
 public:
    inline explicit ManagedRef(JsEngine *engine, int32_t contextId, int id, JsConvert* convert) :
		engine_(engine),
		contextId_(contextId),
		id_(id),
		convert_(convert)
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

    ~ManagedRef() { 
		engine_->CallRemove(contextId_, id_); 
		DECREMENT(js_mem_debug_managedref_count);
	}
    
 private:
    ManagedRef() {
		INCREMENT(js_mem_debug_managedref_count);
	}
	int32_t contextId_;
	JsEngine *engine_;
	int32_t id_;
	JsConvert* convert_;
};

#endif
