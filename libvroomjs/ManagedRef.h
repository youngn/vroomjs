#pragma once

#include "vroomjs.h"

class JsContext;

class ManagedRef
{
public:
    ManagedRef(JsContext* context, int32_t id) :
        context_(context),
        id_(id)
    {
        INCREMENT(js_mem_debug_managedref_count);
    }

    ~ManagedRef();

    int32_t Id() { return id_; }

    static void managed_prop_get(Local<Name> name, const PropertyCallbackInfo<Value>& info);
    static void managed_prop_set(Local<Name> name, Local<Value> value, const PropertyCallbackInfo<Value>& info);
    static void managed_prop_delete(Local<Name> name, const PropertyCallbackInfo<Boolean>& info);
    static void managed_prop_enumerate(const PropertyCallbackInfo<Array>& info);
    static void managed_call(const FunctionCallbackInfo<Value>& info);
    static void managed_valueof(const FunctionCallbackInfo<Value>& info);

private:
    static ManagedRef* GetProxyInstance(const Local<Object>& obj);

    void GetPropertyValue(Local<Name> name, const PropertyCallbackInfo<Value>& info);
    void SetPropertyValue(Local<Name> name, Local<Value> value, const PropertyCallbackInfo<Value>& info);
    void DeleteProperty(Local<Name> name, const PropertyCallbackInfo<Boolean>& info);
    void EnumerateProperties(const PropertyCallbackInfo<Array>& info);
    void Invoke(const FunctionCallbackInfo<Value>& info);
    void GetValueOf(const FunctionCallbackInfo<Value>& info);

    JsContext* context_;
    int32_t id_;
};
