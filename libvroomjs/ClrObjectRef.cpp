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
#include "ClrObjectRef.h"
#include "JsValue.h"
#include "JsContext.h"
#include "ClrObjectCallbacks.h"

using namespace v8;

long js_mem_debug_clrobjectref_count;

void ClrObjectRef::GetPropertyValue(Local<Name> name, const PropertyCallbackInfo<Value>& info)
{
    auto isolate = context_->Isolate();
    String::Value s(isolate, name);

    auto r = callbacks_.GetPropertyValue(context_->Id(), id_, *s);
    if (r.ValueType() == JSVALUE_TYPE_CLRERROR) {
        isolate->ThrowException(r.Extract(context_));
        return;
    }
    info.GetReturnValue().Set(r.Extract(context_));
}

void ClrObjectRef::SetPropertyValue(Local<Name> name, Local<Value> value, const PropertyCallbackInfo<Value>& info)
{
    auto isolate = context_->Isolate();
    String::Value s(isolate, name);

    auto r = callbacks_.SetPropertyValue(context_->Id(), id_, *s, JsValue::ForValue(value, context_));
    if (r.ValueType() == JSVALUE_TYPE_CLRERROR) {
        isolate->ThrowException(r.Extract(context_));
        return;
    }

    // Extract the value so that clean-up is performed
    r.Extract(context_);
}

void ClrObjectRef::DeleteProperty(Local<Name> name, const PropertyCallbackInfo<Boolean>& info)
{
    auto isolate = context_->Isolate();
    String::Value s(isolate, name);

    auto r = callbacks_.DeleteProperty(context_->Id(), id_, *s);
    if (r.ValueType() == JSVALUE_TYPE_CLRERROR) {
        isolate->ThrowException(r.Extract(context_));
        return;
    }

    // todo: check docs if we really need to be returning a value here
    info.GetReturnValue().Set(r.Extract(context_)->ToBoolean(isolate));
}

void ClrObjectRef::EnumerateProperties(const PropertyCallbackInfo<Array>& info)
{
    auto r = callbacks_.EnumerateProperties(context_->Id(), id_);
    if (r.ValueType() == JSVALUE_TYPE_CLRERROR) {
        context_->Isolate()->ThrowException(r.Extract(context_));
        return;
    }

    info.GetReturnValue().Set(Local<Array>::Cast(r.Extract(context_)));
}

void ClrObjectRef::Invoke(const FunctionCallbackInfo<Value>& info)
{
    auto len = info.Length();
    auto args = new jsvalue[len];

    for (auto i = 0; i < len; i++) {
        args[i] = JsValue::ForValue(info[i], context_);
    }

    auto r = callbacks_.Invoke(context_->Id(), id_, len, args);
    delete[] args;

    if (r.ValueType() == JSVALUE_TYPE_CLRERROR) {
        context_->Isolate()->ThrowException(r.Extract(context_));
        return;
    }

    info.GetReturnValue().Set(r.Extract(context_));
}

void ClrObjectRef::ValueOf(const FunctionCallbackInfo<Value>& info)
{
    auto isolate = context_->Isolate();

    JsValue r = callbacks_.ValueOf(context_->Id(), id_);
    if (r.ValueType() == JSVALUE_TYPE_CLRERROR) {
        isolate->ThrowException(r.Extract(context_));
        return;
    }

    info.GetReturnValue().Set(r.Extract(context_));
}

void ClrObjectRef::ToString(const FunctionCallbackInfo<Value>& info)
{
    auto isolate = context_->Isolate();

    JsValue r = callbacks_.ToString(context_->Id(), id_);
    if (r.ValueType() == JSVALUE_TYPE_CLRERROR) {
        isolate->ThrowException(r.Extract(context_));
        return;
    }

    info.GetReturnValue().Set(r.Extract(context_));
}

ClrObjectRef::~ClrObjectRef()
{
    callbacks_.Remove(context_->Id(), id_);
    DECREMENT(js_mem_debug_clrobjectref_count);
}

