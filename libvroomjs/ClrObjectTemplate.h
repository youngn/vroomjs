#pragma once

#include "vroomjs.h"
#include "ClrObjectCallbacks.h"

class ClrObjectRef;

class ClrObjectTemplate
{
public:
    ClrObjectTemplate(Isolate* isolate, jscallbacks callbacks);

    Local<Object> NewInstance(Local<Context> ctx, ClrObjectRef* ref) const;

    const ClrObjectCallbacks& Callbacks() const {
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

    static ClrObjectRef* GetClrObjectRef(Local<Object> obj);

    Isolate* isolate_;
    ClrObjectCallbacks callbacks_;
    UniquePersistent<ObjectTemplate> template_;
};

