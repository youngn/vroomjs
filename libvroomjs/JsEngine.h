#pragma once

#include <vector>
#include <cassert>
#include "vroomjs.h"
#include "Disposable.h"

class JsContext;
class HostObjectTemplate;

// JsEngine is a single isolated v8 interpreter and is the referenced as an IntPtr
// by the JsEngine on the CLR side.
class JsEngine : public Disposable
{
public:
    JsEngine(int32_t max_young_space, int32_t max_old_space);
    void TerminateExecution();

    // Dispose a Persistent<Object> that was held on the CLR side by JsObject.
    void DisposeObject(Persistent<Object>* obj);

    void DumpHeapStats();
    Isolate* Isolate() { return isolate_; }
    JsContext* NewContext(int32_t id);

    int AddTemplate(hostobjectcallbacks callbacks);

    const HostObjectTemplate* Template(int i) {
        assert(i >= 0 && i < templates_.size());
        return templates_.at(i);
    }

    virtual ~JsEngine() {
        DECREMENT(js_mem_debug_engine_count);
    }

protected:
    void DisposeCore() override;

private:
    v8::Isolate* isolate_;
    ArrayBuffer::Allocator* allocator_;

    // We use an array of pointers here to guarantee that each HostObjectTemplate
    // has a stable memory location so that we can maintain long-lived references to it.
    std::vector<HostObjectTemplate*> templates_;
};

