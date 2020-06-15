#pragma once

#include "vroomjs.h"
#include "JsValue.h"
class JsContext;

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
    inline JsValue CallGetPropertyValue(int32_t context, int32_t id, uint16_t* name) {
        if (callbacks_.get_property_value == NULL) {
            jsvalue v;
            v.type == JSVALUE_TYPE_NULL;
            return v;
        }
        jsvalue value = callbacks_.get_property_value(context, id, name);
        return value;
    }
    inline JsValue CallSetPropertyValue(int32_t context, int32_t id, uint16_t* name, jsvalue value) {
        if (callbacks_.set_property_value == NULL) {
            jsvalue v;
            v.type == JSVALUE_TYPE_NULL;
            return v;
        }
        return callbacks_.set_property_value(context, id, name, value);
    }
    inline JsValue CallValueOf(int32_t context, int32_t id) {
        if (callbacks_.valueof == NULL) {
            jsvalue v;
            v.type == JSVALUE_TYPE_NULL;
            return v;
        }
        return callbacks_.valueof(context, id);
    }
    inline JsValue CallInvoke(int32_t context, int32_t id, int32_t argCount, jsvalue* args) {
        if (callbacks_.invoke == NULL) {
            jsvalue v;
            v.type == JSVALUE_TYPE_NULL;
            return v;
        }
        return callbacks_.invoke(context, id, argCount, args);
    }
    inline JsValue CallDeleteProperty(int32_t context, int32_t id, uint16_t* name) {
        if (callbacks_.delete_property == NULL) {
            jsvalue v;
            v.type == JSVALUE_TYPE_NULL;
            return v;
        }
        jsvalue value = callbacks_.delete_property(context, id, name);
        return value;
    }
    inline JsValue CallEnumerateProperties(int32_t context, int32_t id) {
        if (callbacks_.enumerate_properties == NULL) {
            jsvalue v;
            v.type == JSVALUE_TYPE_NULL;
            return v;
        }
        jsvalue value = callbacks_.enumerate_properties(context, id);
        return value;
    }

    Persistent<Script>* CompileScript(const uint16_t* str, const uint16_t* resourceName, jsvalue* error);

    // Dispose a Persistent<Object> that was held on the CLR side by JsObject.
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

