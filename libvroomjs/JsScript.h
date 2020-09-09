#pragma once

#include "vroomjs.h"
#include "Disposable.h"

class JsContext;
class JsValue;

class JsScript : Disposable
{
public:
    JsScript(JsContext* context);

    // This method can only be called once. 
    // (Ideally we would use RAII instead, but we need to be able to have a return value
    // to indicate that compilation was successful).
    JsValue Compile(const uint16_t * code, const uint16_t* resourceName = nullptr);

    JsValue Execute();

    void Dispose() override;
    bool IsDisposed() { return script_ == nullptr; }

    virtual ~JsScript() {
        DECREMENT(js_mem_debug_script_count);
    }

private:
    // Context that owns this object
    JsContext* context_;
    Persistent<Script>* script_;
};

