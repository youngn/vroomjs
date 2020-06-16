// This file is part of the VroomJs library.
//
// Author:
//     Federico Di Gregorio <fog@initd.org>
//
// Copyright © 2013 Federico Di Gregorio <fog@initd.org>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#include <iostream>

#include "vroomjs.h"
#include "ManagedRef.h"
#include "JsValue.h"
#include "JsContext.h"
#include "JsEngine.h"

using namespace v8;

long js_mem_debug_managedref_count;

void ManagedRef::managed_prop_get(Local<Name> name, const PropertyCallbackInfo<Value>& info)
{
#ifdef DEBUG_TRACE_API
    std::cout << "managed_prop_get" << std::endl;
#endif

    GetProxyInstance(info.Holder())->GetPropertyValue(name, info);
}

void ManagedRef::managed_prop_set(Local<Name> name, Local<Value> value, const PropertyCallbackInfo<Value>& info)
{
#ifdef DEBUG_TRACE_API
    std::cout << "managed_prop_set" << std::endl;
#endif

    GetProxyInstance(info.Holder())->SetPropertyValue(name, value, info);
}

void ManagedRef::managed_prop_delete(Local<Name> name, const PropertyCallbackInfo<Boolean>& info)
{
#ifdef DEBUG_TRACE_API
    std::cout << "managed_prop_delete" << std::endl;
#endif

    GetProxyInstance(info.Holder())->DeleteProperty(name, info);
}

void ManagedRef::managed_prop_enumerate(const PropertyCallbackInfo<Array>& info)
{
#ifdef DEBUG_TRACE_API
    std::cout << "managed_prop_enumerate" << std::endl;
#endif

    GetProxyInstance(info.Holder())->EnumerateProperties(info);
}

void ManagedRef::managed_call(const FunctionCallbackInfo<Value>& info)
{
#ifdef DEBUG_TRACE_API
    std::cout << "managed_call" << std::endl;
#endif

    GetProxyInstance(info.Holder())->Invoke(info);
}

void ManagedRef::managed_valueof(const FunctionCallbackInfo<Value>& info)
{
#ifdef DEBUG_TRACE_API
    std::cout << "managed_valueof" << std::endl;
#endif

    GetProxyInstance(info.Holder())->GetValueOf(info);
}

// todo: rename to reflect terminology (ManagedRef != Proxy, Proxy is the JS Object)
ManagedRef* ManagedRef::GetProxyInstance(const Local<Object>& obj)
{
    auto ext = Local<External>::Cast(obj->GetInternalField(0));
    return (ManagedRef*)ext->Value();
}

void ManagedRef::GetPropertyValue(Local<Name> name, const PropertyCallbackInfo<Value>& info)
{
    auto isolate = context_->Isolate();
    String::Value s(isolate, name);

    auto r = context_->ClrObjectCallbacks().GetPropertyValue(context_->Id(), id_, *s);
    if (r.ValueType() == JSVALUE_TYPE_MANAGED_ERROR) {
        isolate->ThrowException(r.Extract(context_));
        return;
    }
    info.GetReturnValue().Set(r.Extract(context_));
}

void ManagedRef::SetPropertyValue(Local<Name> name, Local<Value> value, const PropertyCallbackInfo<Value>& info)
{
    auto isolate = context_->Isolate();
    String::Value s(isolate, name);

    auto r = context_->ClrObjectCallbacks().SetPropertyValue(context_->Id(), id_, *s, JsValue::ForValue(value, context_));
    if (r.ValueType() == JSVALUE_TYPE_MANAGED_ERROR) {
        isolate->ThrowException(r.Extract(context_));
        return;
    }

    r.Extract(context_);
}

void ManagedRef::DeleteProperty(Local<Name> name, const PropertyCallbackInfo<Boolean>& info)
{
    auto isolate = context_->Isolate();
    String::Value s(isolate, name);

    auto r = context_->ClrObjectCallbacks().DeleteProperty(context_->Id(), id_, *s);
    if (r.ValueType() == JSVALUE_TYPE_MANAGED_ERROR) {
        isolate->ThrowException(r.Extract(context_));
        return;
    }

    // todo: check docs if we really need to be returning a value here
    info.GetReturnValue().Set(r.Extract(context_)->ToBoolean(isolate));
}

void ManagedRef::EnumerateProperties(const PropertyCallbackInfo<Array>& info)
{
    auto r = context_->ClrObjectCallbacks().EnumerateProperties(context_->Id(), id_);
    if (r.ValueType() == JSVALUE_TYPE_MANAGED_ERROR) {
        context_->Isolate()->ThrowException(r.Extract(context_));
        return;
    }

    info.GetReturnValue().Set(Local<Array>::Cast(r.Extract(context_)));
}

void ManagedRef::Invoke(const FunctionCallbackInfo<Value>& info)
{
    auto len = info.Length();
    auto args = new jsvalue[len];

    for (auto i = 0; i < len; i++) {
        args[i] = JsValue::ForValue(info[i], context_);
    }

    auto r = context_->ClrObjectCallbacks().Invoke(context_->Id(), id_, len, args);
    delete[] args;

    if (r.ValueType() == JSVALUE_TYPE_MANAGED_ERROR) {
        context_->Isolate()->ThrowException(r.Extract(context_));
        return;
    }

    info.GetReturnValue().Set(r.Extract(context_));
}

void ManagedRef::GetValueOf(const FunctionCallbackInfo<Value>& info)
{
    auto isolate = context_->Isolate();

    JsValue r = context_->ClrObjectCallbacks().ValueOf(context_->Id(), id_);
    if (r.ValueType() == JSVALUE_TYPE_MANAGED_ERROR) {
        isolate->ThrowException(r.Extract(context_));
        return;
    }

    info.GetReturnValue().Set(r.Extract(context_));
}

ManagedRef::~ManagedRef()
{
    context_->ClrObjectCallbacks().Remove(context_->Id(), id_);
    DECREMENT(js_mem_debug_managedref_count);
}

