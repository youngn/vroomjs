#include <cassert>

#include "vroomjs.h"
#include "JsValue.h"
#include "JsContext.h"
#include "JsErrorInfo.h"
#include "ClrObjectRef.h"
#include "ClrObjectManager.h"


JsValue JsValue::ForValue(Local<Value> value, JsContext* context)
{
    auto isolate = context->Isolate();
    auto ctx = context->Ctx();

    if (value->IsNull() || value->IsUndefined()) {
        return ForNull();
    }

    if (value->IsBoolean()) {
        return ForBoolean(value->BooleanValue(isolate));
    }

    if (value->IsInt32()) {
        return ForInt32(value->Int32Value(ctx).FromMaybe(0));
    }

    if (value->IsUint32()) {
        return ForUInt32(value->Uint32Value(ctx).FromMaybe(0));
    }

    if (value->IsNumber()) {
        return ForNumber(value->NumberValue(ctx).FromMaybe(0.0));
    }

    if (value->IsString()) {
        auto s = Local<String>::Cast(value);
        return ForJsString(new Persistent<String>(isolate, s), s->Length());
        //auto len = s->Length();

        //// todo: is this the best way to convert?
        //uint16_t* str = new uint16_t[len + 1];
        //s->Write(isolate, str);
        //return ForString(len, str);
    }

    if (value->IsDate()) {
        return ForDate(value->NumberValue(ctx).FromMaybe(0.0));
    }

    if (value->IsArray()) {
        auto arr = Local<Array>::Cast(value);
        return ForJsArray(new Persistent<Array>(isolate, arr));
    }

    if (value->IsFunction()) {
        auto function = Local<Function>::Cast(value);

        // TODO: why is it that the proxy object is a function? It seems like we shouldn't need this 'if' here
        // (unless the object being proxied is a CLR delegate, in which case it could make sense)
        if (function->InternalFieldCount() > 0) {
            auto ref = (ClrObjectRef*)Local<External>::Cast(function->GetInternalField(0))->Value();
            return ForClrObject(ref->Id());
        }
        else {
            return ForJsFunction(new Persistent<Function>(isolate, function));
        }
    }

    if (value->IsObject()) {
        auto obj = Local<Object>::Cast(value);
        if (obj->InternalFieldCount() > 0) {
            auto ref = (ClrObjectRef*)Local<External>::Cast(obj->GetInternalField(0))->Value();
            return ForClrObject(ref->Id());
        }
        else {
            return ForJsObject(new Persistent<Object>(isolate, obj));
        }
    }

    // Default to a generic error.
    return ForUnknownError();
}

Local<Value> JsValue::GetValue(JsContext* context)
{
    auto isolate = context->Isolate();
    auto ctx = context->Ctx();

    if (ValueType() == JSVALUE_TYPE_EMPTY) {
        return Local<Value>();
    }
    if (ValueType() == JSVALUE_TYPE_NULL) {
        return Null(isolate);
    }
    if (ValueType() == JSVALUE_TYPE_BOOLEAN) {
        return Boolean::New(isolate, BooleanValue());
    }
    if (ValueType() == JSVALUE_TYPE_INTEGER) {
        return Int32::New(isolate, Int32Value());
    }
    if (ValueType() == JSVALUE_TYPE_NUMBER) {
        return Number::New(isolate, NumberValue());
    }
    if (ValueType() == JSVALUE_TYPE_STRING) {
        return String::NewFromTwoByte(isolate, StringValue()).ToLocalChecked();
    }
    if (ValueType() == JSVALUE_TYPE_JSSTRING) {
        auto pObj = JsStringValue();
        return Local<String>::New(isolate, *pObj);
    }
    if (ValueType() == JSVALUE_TYPE_DATE) {
        return Date::New(ctx, DateValue()).ToLocalChecked();
    }
    if (ValueType() == JSVALUE_TYPE_JSOBJECT) {
        auto pObj = JsObjectValue();
        return Local<Object>::New(isolate, *pObj);
    }
    if (ValueType() == JSVALUE_TYPE_JSARRAY) {
        auto pObj = JsArrayValue();
        return Local<Array>::New(isolate, *pObj);
    }
    if (ValueType() == JSVALUE_TYPE_JSFUNCTION) {
        auto pObj = JsFunctionValue();
        return Local<Function>::New(isolate, *pObj);
    }

    if (ValueType() == JSVALUE_TYPE_CLROBJECT || ValueType() == JSVALUE_TYPE_CLRERROR) {
        // This is an ID to a CLR object that lives inside the JsContext keep-alive
        // cache. We just wrap it and the pointer to the engine inside an External. A
        // CLR error is still a CLR object so it is wrapped exactly as a normal
        // CLR object.
        return context->ClrObjectMgr()->GetProxy(ClrObjectIdValue());
    }

    // todo: throw?
    return Null(isolate);
}

JsValue JsValue::ForError(TryCatch& trycatch, JsContext* context)
{
    assert(trycatch.HasCaught()); // an exception has been caught

    auto isolate = context->Isolate();
    auto ctx = context->Ctx();

    HandleScope scope(isolate);

    auto exception = trycatch.Exception();
    assert(!exception.IsEmpty());

    // Is this a CLR exception?
    Local<Object> obj;
    if (exception->ToObject(ctx).ToLocal(&obj) && obj->InternalFieldCount() == 1) {
        auto ref = (ClrObjectRef*)Local<External>::Cast(obj->GetInternalField(0))->Value();
        return JsValue::ForClrError(ref->Id());
    }

    auto errorInfo = JsErrorInfo::Capture(trycatch, context);
    return JsValue::ForError(errorInfo);
}

void JsValue::Dispose()
{
    switch (v.type) {

    case JSVALUE_TYPE_STRING:
        delete[] v.value.str;
        break;

    case JSVALUE_TYPE_JSSTRING:
    {
        assert(v.value.ptr != nullptr);

        auto obj = (Persistent<String>*)v.value.ptr;
        obj->Reset();
        delete obj;
        break;
    }

    case JSVALUE_TYPE_ERROR:
        auto info = (JsErrorInfo*)v.value.ptr;
        delete info;
        break;
    }
}