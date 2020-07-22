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

#include <vector>
#include <iostream>
#include <cassert>

#include "vroomjs.h"
#include "JsContext.h"
#include "JsEngine.h"
#include "JsValue.h"
#include "JsScript.h"
#include "HostObjectRef.h"
#include "HostObjectManager.h"


using namespace v8;

long js_mem_debug_context_count;

JsContext::JsContext(int32_t id, JsEngine* engine)
{
    id_ = id;
    engine_ = engine;
    isolate_ = engine->Isolate();

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    HandleScope scope(isolate_);

    context_ = new Persistent<Context>(isolate_, Context::New(isolate_));

    hostObjectManager_ = new HostObjectManager(this);

    // Do this last, in case anything above fails
    INCREMENT(js_mem_debug_context_count);
}

void JsContext::Dispose()
{
    if (context_ == nullptr)
        return;

    // Was the engine already disposed?
    if (engine_->Isolate() != nullptr) {
        // todo: do we really need the locker/isolate scope?
        Locker locker(isolate_);
        Isolate::Scope isolate_scope(isolate_);
        context_->Reset();
    }
    delete context_;
    context_ = nullptr;

    delete hostObjectManager_;
    hostObjectManager_ = nullptr;

    engine_ = nullptr;
    isolate_ = nullptr;
}

JsValue JsContext::Execute(const uint16_t* str, const uint16_t* resourceName = NULL)
{
    assert(str != nullptr);

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    HandleScope scope(isolate_);

    auto ctx = Local<Context>::New(isolate_, *context_);
    Context::Scope contextScope(ctx);

    TryCatch trycatch(isolate_);

    auto source = String::NewFromTwoByte(isolate_, str).ToLocalChecked();

    auto res_name = resourceName != NULL
        ? String::NewFromTwoByte(isolate_, resourceName).ToLocalChecked()
        : String::Empty(isolate_);

    ScriptOrigin scriptOrigin(res_name);

    Local<Script> script;
    if (!Script::Compile(isolate_->GetCurrentContext(), source, &scriptOrigin).ToLocal(&script))
    {
        // Compilation failed e.g. syntax error
        return JsValue::ForError(trycatch, this);
    }

    Local<Value> result;
    if (script->Run(isolate_->GetCurrentContext()).ToLocal(&result)) {
        return JsValue::ForValue(result, this);
    } else {
        return JsValue::ForError(trycatch, this);
    }
}

JsValue JsContext::Execute(JsScript* jsscript)
{
    assert(jsscript != nullptr);

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    HandleScope scope(isolate_);
    Context::Scope contextScope(Local<Context>::New(isolate_, *context_));

    TryCatch trycatch(isolate_);

    auto script = Local<Script>::New(isolate_, *(jsscript->GetScript()));

    Local<Value> result;
    if (script->Run(isolate_->GetCurrentContext()).ToLocal(&result)) {
        return JsValue::ForValue(result, this);
    } else {
        return JsValue::ForError(trycatch, this);
    }
}

JsValue JsContext::GetGlobal() {

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    HandleScope scope(isolate_);

    auto ctx = Local<Context>::New(isolate_, *context_);
    Context::Scope contextScope(ctx);

    TryCatch trycatch(isolate_);

    Local<Value> value = ctx->Global();
    if (!value.IsEmpty()) {
        return JsValue::ForValue(value, this);
    }
    else {
        return JsValue::ForError(trycatch, this);
    }
}

JsValue JsContext::GetVariable(const uint16_t* name)
{
    assert(name != nullptr);

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    HandleScope scope(isolate_);

    auto ctx = Local<Context>::New(isolate_, *context_);
    Context::Scope contextScope(ctx);

    auto var_name = String::NewFromTwoByte(isolate_, name).ToLocalChecked();

    TryCatch trycatch(isolate_);

    Local<Value> value;
    if (ctx->Global()->Get(ctx, var_name).ToLocal(&value)) {
        return JsValue::ForValue(value, this);
    }
    else {
        return JsValue::ForError(trycatch, this);
    }
}

JsValue JsContext::SetVariable(const uint16_t* name, JsValue value)
{
    assert(name != nullptr);

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    HandleScope scope(isolate_);

    auto ctx = Local<Context>::New(isolate_, *context_);
    Context::Scope contextScope(ctx);

    auto v = value.Extract(this);
    auto var_name = String::NewFromTwoByte(isolate_, name).ToLocalChecked();

    ctx->Global()->Set(ctx, var_name, v).Check();

    // This return value would be needed in order to pass an error back.
    // However, it seems that Set can never fail, so we just return empty.
    return JsValue::ForEmpty();
}

JsValue JsContext::CreateObject()
{
    Locker locker(isolate_);
    HandleScope scope(isolate_);

    auto ctx = Local<Context>::New(isolate_, *context_);
    Context::Scope contextScope(ctx);

    auto obj = Object::New(isolate_);
    return JsValue::ForValue(obj, this);
}

JsValue JsContext::CreateArray(int len, const JsValue * elements)
{
    assert(len >= 0);

    Locker locker(isolate_);
    HandleScope scope(isolate_);

    auto ctx = Local<Context>::New(isolate_, *context_);
    Context::Scope contextScope(ctx);

    auto arr = Array::New(isolate_, len);

    if (elements) {
        auto ctx = Local<Context>::New(isolate_, *context_);
        for (int i = 0; i < len; i++) {
            arr->Set(ctx, i, JsValue(elements[i]).Extract(this)).Check();
        }
    }

    return JsValue::ForValue(arr, this);
}

JsValue JsContext::GetHostObjectProxy(JsValue hostObject)
{
    if (hostObject.ValueType() != JSVALUE_TYPE_HOSTOBJECT)
        return JsValue::ForEmpty();

    Locker locker(isolate_);
    HandleScope scope(isolate_);

    // The extracted value is the proxy. We just need to marshal it back
    // to the other side as a JsObject.
    auto obj = Local<Object>::Cast(hostObject.Extract(this));
    return JsValue::ForJsObject(obj, this);
}
