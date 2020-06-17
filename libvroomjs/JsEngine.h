#pragma once

#include "vroomjs.h"
#include "ClrObjectCallbacks.h"

class JsContext;

// JsEngine is a single isolated v8 interpreter and is the referenced as an IntPtr
// by the JsEngine on the CLR side.
class JsEngine
{
public:
    JsEngine(int32_t max_young_space, int32_t max_old_space, jscallbacks callbacks);
    void TerminateExecution();

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

    const ClrObjectCallbacks& ClrObjectCallbacks() {
        return callbacks_;
    }

    virtual ~JsEngine() {
        DECREMENT(js_mem_debug_engine_count);
    }


    Persistent<Context>* global_context_;

private:
    v8::Isolate* isolate_;
    ArrayBuffer::Allocator* allocator_;

    Persistent<FunctionTemplate>* managed_template_;
    Persistent<FunctionTemplate>* valueof_function_template_;

    ::ClrObjectCallbacks callbacks_;
};

