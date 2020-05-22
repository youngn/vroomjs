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
#include "vroomjs.h"
#include <cassert>

using namespace v8;

long js_mem_debug_context_count;

// todo: could we just build this into the ctor, or at least make the ctor accept params?
JsContext* JsContext::New(int id, JsEngine* engine)
{
    JsContext* context = new JsContext();
    context->id_ = id;
    context->engine_ = engine;
    context->isolate_ = engine->GetIsolate();

    Locker locker(context->isolate_);
    Isolate::Scope isolate_scope(context->isolate_);
    HandleScope scope(context->isolate_);

    context->context_ = new Persistent<Context>(
        context->isolate_, Context::New(context->isolate_));

    return context;
}

void JsContext::Dispose()
{
    if (engine_->GetIsolate() != NULL) {
        Locker locker(isolate_);
        Isolate::Scope isolate_scope(isolate_);
        context_->Reset();
        delete context_;
    }
}

jsvalue JsContext::Execute(const uint16_t* str, const uint16_t* resourceName = NULL)
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
        return engine_->ErrorFromV8(trycatch);
    }

    Local<Value> result;
    if (script->Run(isolate_->GetCurrentContext()).ToLocal(&result)) {
        return engine_->AnyFromV8(result);
    } else {
        return engine_->ErrorFromV8(trycatch);
    }
}

jsvalue JsContext::Execute(JsScript* jsscript)
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
        return engine_->AnyFromV8(result);
    } else {
        return engine_->ErrorFromV8(trycatch);
    }
}

jsvalue JsContext::SetVariable(const uint16_t* name, jsvalue value)
{
    assert(name != nullptr);

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    HandleScope scope(isolate_);

    auto ctx = Local<Context>::New(isolate_, *context_);
    Context::Scope contextScope(ctx);

    auto v = engine_->AnyToV8(value, id_);
    auto var_name = String::NewFromTwoByte(isolate_, name).ToLocalChecked();

    ctx->Global()->Set(ctx, var_name, v).Check();

    // This return value would be needed in order to pass an error back.
    // However, it seems that Set can never fail, so we just return empty.
    jsvalue none;
    none.type = JSVALUE_TYPE_EMPTY;
    return none;
}

jsvalue JsContext::GetGlobal() {
    jsvalue v;

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    HandleScope scope(isolate_);

    auto ctx = Local<Context>::New(isolate_, *context_);
    Context::Scope contextScope(ctx);

    TryCatch trycatch(isolate_);

    Local<Value> value = ctx->Global();
    if (!value.IsEmpty()) {
        v = engine_->AnyFromV8(value);
    } else {
        v = engine_->ErrorFromV8(trycatch);
    }

    return v;
}

jsvalue JsContext::GetVariable(const uint16_t* name)
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
        return engine_->AnyFromV8(value);
    } else {
        return engine_->ErrorFromV8(trycatch);
    }
}

jsvalue JsContext::GetPropertyNames(Persistent<Object>* obj)
{
    assert(obj != nullptr);

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    HandleScope scope(isolate_);

    auto ctx = Local<Context>::New(isolate_, *context_);
    Context::Scope contextScope(ctx);

    TryCatch trycatch(isolate_);

    Local<Object> objLocal = Local<Object>::New(isolate_, *obj);
    Local<Value> value = objLocal->GetPropertyNames(ctx).ToLocalChecked();
    if (!value.IsEmpty()) {
        return engine_->AnyFromV8(value);
    } else {
        return engine_->ErrorFromV8(trycatch);
    }
}

jsvalue JsContext::GetPropertyValue(Persistent<Object>* obj, const uint16_t* name)
{
    assert(obj != nullptr);
    assert(name != nullptr);

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    HandleScope scope(isolate_);

    auto ctx = Local<Context>::New(isolate_, *context_);
    Context::Scope contextScope(ctx);

    auto var_name = String::NewFromTwoByte(isolate_, name).ToLocalChecked();

    auto objLocal = Local<Object>::New(isolate_, *obj);

    TryCatch trycatch(isolate_);

    Local<Value> value;
    if (objLocal->Get(ctx, var_name).ToLocal(&value)) {
        return engine_->AnyFromV8(value);
    } else {
        return engine_->ErrorFromV8(trycatch);
    }
}

jsvalue JsContext::SetPropertyValue(Persistent<Object>* obj, const uint16_t* name, jsvalue value)
{
    assert(obj != nullptr);
    assert(name != nullptr);

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    HandleScope scope(isolate_);

    auto ctx = Local<Context>::New(isolate_, *context_);
    Context::Scope contextScope(ctx);

    auto v = engine_->AnyToV8(value, id_);
    auto var_name = String::NewFromTwoByte(isolate_, name).ToLocalChecked();
    auto objLocal = Local<Object>::New(isolate_, *obj);

    objLocal->Set(ctx, var_name, v).Check();

    // This return value would be needed in order to pass an error back.
    // However, it seems that Set can never fail, so we just return empty.
    jsvalue none;
    none.type = JSVALUE_TYPE_EMPTY;
    return none;
}

jsvalue JsContext::InvokeFunction(Persistent<Function>* func, Persistent<Object>* thisArg, jsvalue args) {
    assert(func != nullptr);
    assert(thisArg != nullptr);
    assert(args.type == JSVALUE_TYPE_ARRAY);

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    HandleScope scope(isolate_);

    auto ctx = Local<Context>::New(isolate_, *context_);
    Context::Scope contextScope(ctx);

    auto funcLocal = Local<Function>::New(isolate_, *func);

    // todo: this seems odd - can it really not be a function, given funcLocal is Local<Function> ??
    // If we could move this check further upstream, we may not need JSVALUE_TYPE_STRING_ERROR (it has very few uses)
    if (funcLocal.IsEmpty() || !funcLocal->IsFunction()) {

        auto str_value = String::NewFromUtf8(isolate_, "isn't a function").ToLocalChecked();
        jsvalue v = engine_->StringFromV8(str_value);
        v.type = JSVALUE_TYPE_STRING_ERROR;
        return v;
    }

    TryCatch trycatch(isolate_);

    auto receiver = Local<Object>::New(isolate_, *thisArg);

    // todo: can receiver really be empty?
    if (receiver.IsEmpty()) {
        receiver = ctx->Global();
    }

    // todo: could we potentially pass an array of jsvalues into this function?
    // i.e. as opposed to assuming we're dealing with a JS array for the args vector
    std::vector<Local<Value>> argv(args.length);
    engine_->ArrayToV8Args(args, id_, &argv[0]);

    // TODO: Check ArrayToV8Args return value (but right now can't fail, right?)

    auto funcLocal2 = Local<Function>::Cast(funcLocal);

    Local<Value> retVal;
    if (funcLocal2->Call(ctx, receiver, args.length, &argv[0]).ToLocal(&retVal)) {
        return engine_->AnyFromV8(retVal);
    } else {
        return engine_->ErrorFromV8(trycatch);
    }
}

jsvalue JsContext::InvokeProperty(Persistent<Object>* obj, const uint16_t* name, jsvalue args)
{
    assert(obj != nullptr);
    assert(name != nullptr);
    assert(args.type == JSVALUE_TYPE_ARRAY);

    Locker locker(isolate_);
    Isolate::Scope isolate_scope(isolate_);
    HandleScope scope(isolate_);

    auto ctx = Local<Context>::New(isolate_, *context_);
    Context::Scope contextScope(ctx);

    auto var_name = String::NewFromTwoByte(isolate_, name).ToLocalChecked();

    TryCatch trycatch(isolate_);

    auto objLocal = Local<Object>::New(isolate_, *obj);
    Local<Value> propValue;
    if (!objLocal->Get(ctx, var_name).ToLocal(&propValue) || !propValue->IsFunction()) {

        auto str_value = String::NewFromUtf8(isolate_, "property not found or isn't a function").ToLocalChecked();
        jsvalue v = engine_->StringFromV8(str_value);
        v.type = JSVALUE_TYPE_STRING_ERROR;
        return v;
    }

    // todo: could we potentially pass an array of jsvalues into this function?
    // i.e. as opposed to assuming we're dealing with a JS array for the args vector
    std::vector<Local<Value>> argv(args.length);
    engine_->ArrayToV8Args(args, id_, &argv[0]);
    // TODO: Check ArrayToV8Args return value (but right now can't fail, right?)   

    auto funcLocal2 = Local<Function>::Cast(propValue);

    Local<Value> value;
    if (funcLocal2->Call(ctx, objLocal, args.length, &argv[0]).ToLocal(&value)) {
        return engine_->AnyFromV8(value);
    } else {
        return engine_->ErrorFromV8(trycatch);
    }
}



