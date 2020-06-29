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
    JsValue GetPropertyNames(Persistent<Object>* obj);
    JsValue GetPropertyValue(Persistent<Object>* obj, const uint16_t* name);
    JsValue GetPropertyValue(Persistent<Object>* obj, const uint32_t index);
    JsValue SetPropertyValue(Persistent<Object>* obj, const uint16_t* name, JsValue value);
    JsValue SetPropertyValue(Persistent<Object>* obj, const uint32_t index, JsValue value);
    JsValue InvokeFunction(Persistent<Function>* func, JsValue receiver, int argCount, JsValue* args);

    JsValue CreateArray(int len, const jsvalue* elements);

    void Dispose();

    int32_t Id() { return id_; }
    JsEngine* Engine() { return engine_; }
    Isolate* Isolate() { return isolate_; }
    Local<Context> Ctx() {
        return Local<Context>::New(isolate_, *context_);
    }

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

