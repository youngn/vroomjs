#pragma once

#include "vroomjs.h"

class JsContext;
class ClrObjectCallbacks;

class ClrObjectRef
{
public:
    ClrObjectRef(JsContext* context, int32_t id, const ClrObjectCallbacks& callbacks) :
        context_(context),
        id_(id),
        callbacks_(callbacks)
    {
        INCREMENT(js_mem_debug_clrobjectref_count);
    }

    ~ClrObjectRef();

    int32_t Id() { return id_; }

    void GetPropertyValue(Local<Name> name, const PropertyCallbackInfo<Value>& info);
    void SetPropertyValue(Local<Name> name, Local<Value> value, const PropertyCallbackInfo<Value>& info);
    void DeleteProperty(Local<Name> name, const PropertyCallbackInfo<Boolean>& info);
    void EnumerateProperties(const PropertyCallbackInfo<Array>& info);
    void Invoke(const FunctionCallbackInfo<Value>& info);
    void ValueOf(const FunctionCallbackInfo<Value>& info);
    void ToString(const FunctionCallbackInfo<Value>& info);

private:
    JsContext* context_;
    int32_t id_;
    const ClrObjectCallbacks& callbacks_;
};
