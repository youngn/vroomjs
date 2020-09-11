#include <cassert>

#include "JsValue.h"
#include "JsContext.h"
#include "JsErrorInfo.h"
#include "HostObjectRef.h"
#include "HostObjectManager.h"
#include "JsObject.h"
#include "JsFunction.h"
#include "JsArray.h"
#include "JsScript.h"


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
        return ForJsString(Local<String>::Cast(value), context);
    }

    if (value->IsDate()) {
        return ForDate(value->NumberValue(ctx).FromMaybe(0.0));
    }

    if (value->IsArray()) {
        return ForJsArray(Local<Array>::Cast(value), context);
    }

    if (value->IsFunction()) {
        auto function = Local<Function>::Cast(value);

        // Is the function a proxied Host object?
        if (function->InternalFieldCount() > 0) {
            auto ref = (HostObjectRef*)Local<External>::Cast(function->GetInternalField(0))->Value();
            return ForHostObject(ref->Id());
        }
        else {
            return ForJsFunction(function, context);
        }
    }

    if (value->IsObject()) {
        auto obj = Local<Object>::Cast(value);

        // Is the object a proxied Host object?
        if (obj->InternalFieldCount() > 0) {
            auto ref = (HostObjectRef*)Local<External>::Cast(obj->GetInternalField(0))->Value();
            return ForHostObject(ref->Id());
        }
        else {
            return ForJsObject(obj, context);
        }
    }

    // todo: other primitive types?
    //  Symbol

    // Should never get here because all possible JS types should be covered above.
    assert(false);
    return ForEmpty();
}

JsValue JsValue::ForError(TryCatch& trycatch, JsContext* context)
{
    // Handle case where execution was forcibly terminated. 
    if (trycatch.HasTerminated())
        return JsValue::ForTermination();

    auto errorInfo = JsErrorInfo::Capture(trycatch, context);
    return JsValue::ForError(errorInfo);
}

JsValue JsValue::ForJsString(Local<String> value, JsContext* context)
{
    assert(!value.IsEmpty());
    assert(context != nullptr);
    return JsValue(JSVALUE_TYPE_JSSTRING, value->Length(), (void*)new Persistent<String>(context->Isolate(), value));
}

inline JsValue JsValue::ForJsArray(Local<Array> value, JsContext* context) {
    assert(!value.IsEmpty());
    assert(context != nullptr);
    return JsValue(JSVALUE_TYPE_JSARRAY, 0, (void*)new JsArray(value, context));
}

inline JsValue JsValue::ForJsFunction(Local<Function> value, JsContext* context) {
    assert(!value.IsEmpty());
    assert(context != nullptr);
    return JsValue(JSVALUE_TYPE_JSFUNCTION, 0, (void*)new JsFunction(value, context));
}

inline JsValue JsValue::ForJsObject(Local<Object> value, JsContext* context) {
    assert(!value.IsEmpty());
    assert(context != nullptr);
    return JsValue(JSVALUE_TYPE_JSOBJECT, 0, (void*)new JsObject(value, context));
}

JsValue JsValue::ForScript(JsScript* script)
{
    assert(script != nullptr);
    return JsValue(JSVALUE_TYPE_SCRIPT, 0, (void*)script);
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
        return JsObjectValue()->ToLocal();
    }
    if (ValueType() == JSVALUE_TYPE_JSARRAY) {
        return JsArrayValue()->ToLocal();
    }
    if (ValueType() == JSVALUE_TYPE_JSFUNCTION) {
        return JsFunctionValue()->ToLocal();
    }
    if (ValueType() == JSVALUE_TYPE_HOSTERROR) {
        return HostErrorValue()->ToLocal();
    }
    if (ValueType() == JSVALUE_TYPE_HOSTOBJECT) {
        return context->HostObjectMgr()->GetProxy(HostObjectIdValue(), HostObjectTemplateIdValue());
    }

    // should never get here! (unknown value type)
    assert(false);
    return Undefined(isolate);
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

    case JSVALUE_TYPE_JSERROR:
        auto info = (JsErrorInfo*)v.value.ptr;
        delete info;
        break;
    }
}