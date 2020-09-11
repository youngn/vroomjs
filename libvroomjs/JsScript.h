#pragma once

#include "vroomjs.h"
#include "Disposable.h"

class JsContext;
class JsValue;

class JsScript : public Disposable
{
public:
    JsScript(Local<Script> script, JsContext* context);

    JsValue Execute();

    virtual ~JsScript() {
        DECREMENT(js_mem_debug_script_count);
    }

protected:
    void DisposeCore() override;

private:
    // Context that owns this object
    JsContext* context_;
    Persistent<Script> script_;
};

