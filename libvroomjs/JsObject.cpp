#include "JsObject.h"
#include "JsValue.h"
#include "JsContext.h"


JsObject::JsObject(Local<Object> obj, JsContext* context)
    :obj_(context->Isolate(), obj),
    context_(context)
{
    assert(context != nullptr);
}

Local<Object> JsObject::ToLocal()
{
    return Local<Object>::New(context_->Isolate(), obj_);
}

void JsObject::Dispose()
{
    // TODO: we can't call Reset() on a Persistent whose Isolate has been destroyed.
    // Problem is, we have no way of knowing if our Isolated has been destroyed unless
    // we maintain a weak ptr to it.

    // todo: not sure we actually need this stuff just to Reset a Persistent handle
    //auto isolate = context_->Isolate();
    //Locker locker(isolate);
    //Isolate::Scope isolate_scope(isolate);
    //obj_.Reset();
}

JsValue JsObject::GetPropertyNames()
{
    auto isolate = context_->Isolate();

    Locker locker(isolate);
    Isolate::Scope isolate_scope(isolate);
    HandleScope scope(isolate);

    auto ctx = context_->Ctx();
    Context::Scope contextScope(ctx);

    TryCatch trycatch(isolate);

    auto objLocal = Local<Object>::New(isolate, obj_);
    auto names = objLocal->GetPropertyNames(ctx).ToLocalChecked();
    if (!names.IsEmpty()) {
        return JsValue::ForValue(names, context_);
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

    auto n = String::NewFromTwoByte(isolate, name).ToLocalChecked();
    auto objLocal = Local<Object>::New(isolate, obj_);

    TryCatch trycatch(isolate);

    Local<Value> value;
    if (objLocal->Get(ctx, n).ToLocal(&value)) {
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

    auto objLocal = Local<Object>::New(isolate, obj_);

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
    auto n = String::NewFromTwoByte(isolate, name).ToLocalChecked();
    auto objLocal = Local<Object>::New(isolate, obj_);

    objLocal->Set(ctx, n, v).Check();

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
    auto objLocal = Local<Object>::New(isolate, obj_);

    objLocal->Set(ctx, index, v).Check();

    // This return value would be needed in order to pass an error back.
    // However, it seems that Set can never fail, so we just return empty.
    return JsValue::ForEmpty();
}
