#include <iostream>
#include "vroomjs.h"
#include <cassert>

using namespace v8;

JsConvert::JsConvert(Isolate* isolate) {
    assert(isolate != nullptr);
    this->isolate_ = isolate;
}

JsValue JsConvert::AnyFromV8(Local<Value> value, Local<Object> thisArg) const
{
    if (value->IsNull() || value->IsUndefined()) {
       return JsValue::FromNull();
    }

    if (value->IsBoolean()) {
        return JsValue::FromBoolean(value->BooleanValue(isolate_));
    }

    if (value->IsInt32()) {
        return JsValue::FromInt32(value->Int32Value(isolate_->GetCurrentContext()).FromMaybe(0));
    }

    if (value->IsUint32()) {
        return JsValue::FromUInt32(value->Uint32Value(isolate_->GetCurrentContext()).FromMaybe(0));
    }

    if (value->IsNumber()) {
        return JsValue::FromNumber(value->NumberValue(isolate_->GetCurrentContext()).FromMaybe(0.0));
    }

    if (value->IsString()) {
        //v = StringFromV8(Local<String>::Cast(value));
        auto s = Local<String>::Cast(value);
        auto len = s->Length();

        // todo: is this the best way to convert?
        uint16_t* str = new uint16_t[len + 1];
        s->Write(isolate_, str);
        return JsValue::FromString(len, str);
    }

    if (value->IsDate()) {
        return JsValue::FromDate(value->NumberValue(isolate_->GetCurrentContext()).FromMaybe(0.0));
    }

    if (value->IsArray()) {
        auto arr = Local<Array>::Cast(value);
        return JsValue::FromJsArray(new Persistent<Array>(isolate_, arr));
    }

    if (value->IsFunction()) {
        auto function = Local<Function>::Cast(value);
        return JsValue::FromJsFunction(new Persistent<Function>(isolate_, function));
    }

    if (value->IsObject()) {
        auto obj = Local<Object>::Cast(value);
        if (obj->InternalFieldCount() == 1) {
            //v = ManagedFromV8(obj);
        }
        else {
            return JsValue::FromJsObject(new Persistent<Object>(isolate_, obj));
        }
    }

    // Default to a generic error.
    return JsValue::FromUnknownError();
}

Local<Value> JsConvert::AnyToV8(JsValue value, int32_t contextId) const
{
    if (value.ValueType() == JSVALUE_TYPE_EMPTY) {
        return Local<Value>();
    }
    if (value.ValueType() == JSVALUE_TYPE_NULL) {
        return Null(isolate_);
    }
    if (value.ValueType() == JSVALUE_TYPE_BOOLEAN) {
        return Boolean::New(isolate_, value.BooleanValue());
    }
    if (value.ValueType() == JSVALUE_TYPE_INTEGER) {
        return Int32::New(isolate_, value.Int32Value());
    }
    if (value.ValueType() == JSVALUE_TYPE_NUMBER) {
        return Number::New(isolate_, value.NumberValue());
    }
    if (value.ValueType() == JSVALUE_TYPE_STRING) {
        return String::NewFromTwoByte(isolate_, value.StringValue()).ToLocalChecked();
    }
    if (value.ValueType() == JSVALUE_TYPE_DATE) {
        return Date::New(isolate_->GetCurrentContext(), value.DateValue()).ToLocalChecked();
    }
    if (value.ValueType() == JSVALUE_TYPE_JSOBJECT) {
        auto pObj = value.JsObjectValue();
        return Local<Object>::New(isolate_, *pObj);
    }
    if (value.ValueType() == JSVALUE_TYPE_JSARRAY) {
        auto pObj = value.JsArrayValue();
        return Local<Array>::New(isolate_, *pObj);
    }
    if (value.ValueType() == JSVALUE_TYPE_FUNCTION) {
        auto pObj = value.JsFunctionValue();
        return Local<Function>::New(isolate_, *pObj);
    }

    // Arrays are converted to JS native arrays.
    if (value.ValueType() == JSVALUE_TYPE_ARRAY) {
        //auto arr = Array::New(isolate_, v.length);
        //for(int i = 0; i < v.length; i++) {
        //    arr->Set(isolate_->GetCurrentContext(), i, AnyToV8(v.value.arr[i], contextId));
        //}
        //return arr;        
    }

    // This is an ID to a managed object that lives inside the JsContext keep-alive
    // cache. We just wrap it and the pointer to the engine inside an External. A
    // managed error is still a CLR object so it is wrapped exactly as a normal
    // managed object.
    if (value.ValueType() == JSVALUE_TYPE_MANAGED || value.ValueType() == JSVALUE_TYPE_MANAGED_ERROR) {

        //auto ref = new ManagedRef(this, contextId, v.length);
        //auto t = Local<FunctionTemplate>::New(isolate_, *managed_template_);

        //auto obj = t->InstanceTemplate()->NewInstance(isolate_->GetCurrentContext()).ToLocalChecked();
        //obj->SetInternalField(0, External::New(isolate_, ref));

        // todo: not sure if any of this is needed, revisit
        //Persistent<Object> persistent = Persistent<Object>::New(object);
        //persistent->SetInternalField(0, External::New(ref));
        //persistent.MakeWeak(NULL, managed_destroy);
        //return persistent;
        //return obj;
    }

    // todo: throw?
    return Null(isolate_);
}

JsValue JsConvert::ErrorFromV8(TryCatch& trycatch) const
{
    HandleScope scope(isolate_);

    auto exception = trycatch.Exception();

    // If this is a managed exception we need to place its ID inside the jsvalue
    // and set the type JSVALUE_TYPE_MANAGED_ERROR to make sure the CLR side will
    // throw on it.
    if (exception->IsObject()) {
        auto obj = Local<Object>::Cast(exception);
        if (obj->InternalFieldCount() == 1) {
            Local<External> wrap = Local<External>::Cast(obj->GetInternalField(0));
            ManagedRef* ref = (ManagedRef*)wrap->Value();
            return JsValue::FromManagedError(ref->Id());
        }
    }

    jserror* error = new jserror();
    memset(error, 0, sizeof(jserror));

    auto message = trycatch.Message();

    if (!message.IsEmpty()) {
        error->line = message->GetLineNumber(isolate_->GetCurrentContext()).FromMaybe(0);
        error->column = message->GetStartColumn();
        error->resource = AnyFromV8(message->GetScriptResourceName());
        error->message = AnyFromV8(message->Get());
    }
    if (exception->IsObject()) {
        Local<Object> obj2 = Local<Object>::Cast(exception);
        error->type = AnyFromV8(obj2->GetConstructorName());
    }

    error->exception = AnyFromV8(exception);
    return JsValue::FromError(error);
}


