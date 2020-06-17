#pragma once

#include "vroomjs.h"

class JsContext;

class ClrObjectRef
{
public:
    ClrObjectRef(JsContext* context, int32_t id) :
        context_(context),
        id_(id)
    {
        INCREMENT(js_mem_debug_clrobjectref_count);
    }

    ~ClrObjectRef();

    int32_t Id() { return id_; }

    static void GetPropertyValueCallback(Local<Name> name, const PropertyCallbackInfo<Value>& info);
    static void SetPropertyValueCallback(Local<Name> name, Local<Value> value, const PropertyCallbackInfo<Value>& info);
    static void DeletePropertyCallback(Local<Name> name, const PropertyCallbackInfo<Boolean>& info);
    static void EnumeratePropertiesCallback(const PropertyCallbackInfo<Array>& info);
    static void InvokeCallback(const FunctionCallbackInfo<Value>& info);
    static void ValueOfCallback(const FunctionCallbackInfo<Value>& info);

private:
    static ClrObjectRef* GetInstance(const Local<Object>& obj);

    void GetPropertyValue(Local<Name> name, const PropertyCallbackInfo<Value>& info);
    void SetPropertyValue(Local<Name> name, Local<Value> value, const PropertyCallbackInfo<Value>& info);
    void DeleteProperty(Local<Name> name, const PropertyCallbackInfo<Boolean>& info);
    void EnumerateProperties(const PropertyCallbackInfo<Array>& info);
    void Invoke(const FunctionCallbackInfo<Value>& info);
    void ValueOf(const FunctionCallbackInfo<Value>& info);

    JsContext* context_;
    int32_t id_;
};
