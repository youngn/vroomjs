#pragma once

#include "vroomjs.h"

class JsEngine;
class JsScript;
class JsValue;
class HostObjectManager;

class JsContext
{
public:
    JsContext(int32_t id, JsEngine* engine);

    JsValue Execute(const uint16_t* str, const uint16_t* resourceName);
    JsValue Execute(JsScript* script);

    JsValue GetGlobal();
    JsValue GetVariable(const uint16_t* name);
    JsValue SetVariable(const uint16_t* name, JsValue value);

    JsValue CreateObject();
    JsValue CreateArray(int len, const JsValue* elements);

    void Dispose();

    int32_t Id() { return id_; }
    JsEngine* Engine() { return engine_; }
    Isolate* Isolate() { return isolate_; }
    Local<Context> Ctx() {
        return Local<Context>::New(isolate_, *context_);
    }

    // todo: rename this
    HostObjectManager* HostObjectMgr() {
        return hostObjectManager_;
    }

    ~JsContext() {
        DECREMENT(js_mem_debug_context_count);
    }

private:
    int32_t id_;
    JsEngine* engine_;
    v8::Isolate* isolate_;
    Persistent<Context>* context_;
    ::HostObjectManager* hostObjectManager_;
};

