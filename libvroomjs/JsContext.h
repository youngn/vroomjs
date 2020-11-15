#pragma once

#include <vector>
#include "vroomjs.h"
#include "Disposable.h"

class JsEngine;
class JsScript;
class JsValue;
class HostObjectManager;
class HostObjectTemplate;

class JsContext : public Disposable
{
public:
    JsContext(JsEngine* engine);

    JsValue Execute(const uint16_t* code, const uint16_t* resourceName = nullptr);

    JsValue GetGlobal();
    JsValue GetVariable(const uint16_t* name);
    JsValue SetVariable(const uint16_t* name, JsValue value);

    JsValue CreateObject();
    JsValue CreateArray(int len, const JsValue* elements);
    JsValue GetHostObjectProxy(JsValue hostObject);

    JsValue CompileScript(const uint16_t* code, const uint16_t* resourceName);

    Isolate* Isolate() { return isolate_; }
    Local<Context> Ctx() {
        return Local<Context>::New(isolate_, context_);
    }

    // todo: rename this
    HostObjectManager* HostObjectMgr() {
        return hostObjectManager_;
    }

    int AddTemplate(hostobjectcallbacks callbacks);

    const HostObjectTemplate* Template(int i) {
        assert(i >= 0 && i < templates_.size());
        return templates_.at(i);
    }

    virtual ~JsContext() {
        DECREMENT(js_mem_debug_context_count);
    }

protected:
    void DisposeCore() override;

private:
    JsEngine* engine_;
    v8::Isolate* isolate_;
    Persistent<Context> context_;
    ::HostObjectManager* hostObjectManager_;

    // We use an array of pointers here to guarantee that each HostObjectTemplate
    // has a stable memory location so that we can maintain long-lived references to it.
    // (i.e. if the vector contained the objects, they would move if the vector was
    // resized)
    std::vector<HostObjectTemplate*> templates_;
};

