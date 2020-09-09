#pragma once

#include "vroomjs.h"
#include "Disposable.h"

class JsValue;
class JsContext;

class JsObject : Disposable
{
public:
    JsObject(Local<Object> obj, JsContext* context);

    Local<Object> ToLocal();
    void Dispose() override;

    JsValue GetPropertyNames();
    JsValue GetPropertyValue(const uint16_t* name);
    JsValue GetPropertyValue(const uint32_t index);
    JsValue SetPropertyValue(const uint16_t* name, JsValue value);
    JsValue SetPropertyValue(const uint32_t index, JsValue value);

protected:
    JsContext* Context() { return context_; }
    Persistent<Object>& Obj() { return obj_; }

private:
    Persistent<Object> obj_;
    JsContext* context_;
};

