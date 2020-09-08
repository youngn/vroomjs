#pragma once

#include "vroomjs.h"

class JsContext;
class JsValue;

class JsScript
{
public:
    JsScript(JsContext* context);

    JsValue Compile(const uint16_t* str, const uint16_t* resourceName);

    void Dispose();
    bool IsDisposed() { return script_ == nullptr; }

    Persistent<Script>* GetScript() { return script_; }

    ~JsScript() {
        DECREMENT(js_mem_debug_script_count);
    }

private:
    // Context that owns this object
    JsContext* context_;
    Persistent<Script>* script_;
};

