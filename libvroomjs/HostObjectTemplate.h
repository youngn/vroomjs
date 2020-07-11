#pragma once

#include "vroomjs.h"
#include "HostObjectCallbacks.h"

class HostObjectRef;

class HostObjectTemplate
{
public:
    HostObjectTemplate(Isolate* isolate, hostobjectcallbacks callbacks);

    Local<Object> NewInstance(Local<Context> ctx, HostObjectRef* ref) const;

    const HostObjectCallbacks& Callbacks() const {
        return callbacks_;
    }

private:
    static void GetPropertyValueCallback(Local<Name> name, const PropertyCallbackInfo<Value>& info);
    static void SetPropertyValueCallback(Local<Name> name, Local<Value> value, const PropertyCallbackInfo<Value>& info);
    static void DeletePropertyCallback(Local<Name> name, const PropertyCallbackInfo<Boolean>& info);
    static void EnumeratePropertiesCallback(const PropertyCallbackInfo<Array>& info);
    static void InvokeCallback(const FunctionCallbackInfo<Value>& info);
    static void ValueOfCallback(const FunctionCallbackInfo<Value>& info);
    static void ToStringCallback(const FunctionCallbackInfo<Value>& info);

    static HostObjectRef* GetHostObjectRef(Local<Object> obj);

    Isolate* isolate_;
    HostObjectCallbacks callbacks_;
    UniquePersistent<ObjectTemplate> template_;
};

