#pragma once

#include "vroomjs.h"

class JsContext;
class HostObjectCallbacks;

class HostObjectRef
{
public:
    HostObjectRef(JsContext* context, int32_t id, const HostObjectCallbacks& callbacks) :
        context_(context),
        id_(id),
        callbacks_(callbacks)
    {
        INCREMENT(js_mem_debug_hostobject_count);
    }

    virtual ~HostObjectRef()
    {
        DECREMENT(js_mem_debug_hostobject_count);
    }

    int32_t Id() { return id_; }

    void GetPropertyValue(Local<Name> name, const PropertyCallbackInfo<Value>& info);
    void SetPropertyValue(Local<Name> name, Local<Value> value, const PropertyCallbackInfo<Value>& info);
    void DeleteProperty(Local<Name> name, const PropertyCallbackInfo<Boolean>& info);
    void EnumerateProperties(const PropertyCallbackInfo<Array>& info);
    void Invoke(const FunctionCallbackInfo<Value>& info);
    void ValueOf(const FunctionCallbackInfo<Value>& info);
    void ToString(const FunctionCallbackInfo<Value>& info);

    void NotifyReleased();

private:
    JsContext* context_;
    int32_t id_;
    const HostObjectCallbacks& callbacks_;
};
