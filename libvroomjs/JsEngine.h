#pragma once

#include <vector>
#include <cassert>
#include "vroomjs.h"

class JsContext;
class ClrObjectTemplate;

// JsEngine is a single isolated v8 interpreter and is the referenced as an IntPtr
// by the JsEngine on the CLR side.
class JsEngine
{
public:
    JsEngine(int32_t max_young_space, int32_t max_old_space);
    void TerminateExecution();

    Persistent<Script>* CompileScript(const uint16_t* str, const uint16_t* resourceName, jsvalue* error);

    // Dispose a Persistent<Object> that was held on the CLR side by JsObject.
    void DisposeObject(Persistent<Object>* obj);

    void Dispose();

    void DumpHeapStats();
    Isolate* Isolate() { return isolate_; }
    JsContext* NewContext(int32_t id);

    int AddTemplate(jscallbacks callbacks);

    const ClrObjectTemplate* Template(int i) {
        assert(i >= 0 && i < templates_.size());
        return templates_.at(i);
    }

    virtual ~JsEngine() {
        DECREMENT(js_mem_debug_engine_count);
    }

    Persistent<Context>* global_context_;

private:
    v8::Isolate* isolate_;
    ArrayBuffer::Allocator* allocator_;

    // We use an array of pointers here to guarantee that each ClrObjectTemplate
    // has a stable memory location so that we can maintain long-lived references to it.
    std::vector<ClrObjectTemplate*> templates_;
};

