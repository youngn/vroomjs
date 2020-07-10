#include "JsObject.h"
#include "JsValue.h"
#include "JsContext.h"


JsValue JsObject::GetPropertyNames()
{
    auto isolate = context_->Isolate();

    Locker locker(isolate);
    Isolate::Scope isolate_scope(isolate);
    HandleScope scope(isolate);

    auto ctx = context_->Ctx();
    Context::Scope contextScope(ctx);

    TryCatch trycatch(isolate);

    Local<Object> objLocal = Local<Object>::New(isolate, *obj_);
    Local<Value> value = objLocal->GetPropertyNames(ctx).ToLocalChecked();
    if (!value.IsEmpty()) {
        return JsValue::ForValue(value, context_);
    }
    else {
        return JsValue::ForError(trycatch, context_);
    }
}

JsValue JsObject::GetPropertyValue(const uint16_t* name)
{
    assert(name != nullptr);

    auto isolate = context_->Isolate();

    Locker locker(isolate);
    Isolate::Scope isolate_scope(isolate);
    HandleScope scope(isolate);

    auto ctx = context_->Ctx();
    Context::Scope contextScope(ctx);

    auto var_name = String::NewFromTwoByte(isolate, name).ToLocalChecked();

    auto objLocal = Local<Object>::New(isolate, *obj_);

    TryCatch trycatch(isolate);

    Local<Value> value;
    if (objLocal->Get(ctx, var_name).ToLocal(&value)) {
        return JsValue::ForValue(value, context_);
    }
    else {
        return JsValue::ForError(trycatch, context_);
    }
}

JsValue JsObject::GetPropertyValue(const uint32_t index)
{
    auto isolate = context_->Isolate();

    Locker locker(isolate);
    Isolate::Scope isolate_scope(isolate);
    HandleScope scope(isolate);

    auto ctx = context_->Ctx();
    Context::Scope contextScope(ctx);

    auto objLocal = Local<Object>::New(isolate, *obj_);

    TryCatch trycatch(isolate);

    Local<Value> value;
    if (objLocal->Get(ctx, index).ToLocal(&value)) {
        return JsValue::ForValue(value, context_);
    }
    else {
        return JsValue::ForError(trycatch, context_);
    }
}

JsValue JsObject::SetPropertyValue(const uint16_t* name, JsValue value)
{
    assert(name != nullptr);

    auto isolate = context_->Isolate();

    Locker locker(isolate);
    Isolate::Scope isolate_scope(isolate);
    HandleScope scope(isolate);

    auto ctx = context_->Ctx();
    Context::Scope contextScope(ctx);

    auto v = value.Extract(context_);
    auto var_name = String::NewFromTwoByte(isolate, name).ToLocalChecked();
    auto objLocal = Local<Object>::New(isolate, *obj_);

    objLocal->Set(ctx, var_name, v).Check();

    // This return value would be needed in order to pass an error back.
    // However, it seems that Set can never fail, so we just return empty.
    return JsValue::ForEmpty();
}

JsValue JsObject::SetPropertyValue(const uint32_t index, JsValue value)
{
    auto isolate = context_->Isolate();

    Locker locker(isolate);
    Isolate::Scope isolate_scope(isolate);
    HandleScope scope(isolate);

    auto ctx = context_->Ctx();
    Context::Scope contextScope(ctx);

    auto v = value.Extract(context_);
    auto objLocal = Local<Object>::New(isolate, *obj_);

    objLocal->Set(ctx, index, v).Check();

    // This return value would be needed in order to pass an error back.
    // However, it seems that Set can never fail, so we just return empty.
    return JsValue::ForEmpty();
}
